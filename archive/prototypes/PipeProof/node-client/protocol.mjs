import { decodeUtf8 } from "./frame-codec.mjs";

export const PROTOCOL_NAME = "square.rpc";
export const CURRENT_VERSION = "1.0";
export const SUPPORTED_VERSIONS = Object.freeze([CURRENT_VERSION]);

const schemas = Object.freeze({
  hello: { required: ["kind", "protocol", "version", "id", "client", "capabilities"], optional: [] },
  hello_ack: { required: ["kind", "protocol", "version", "reply_to", "server", "capabilities", "limits", "minimum_available_sequence", "latest_sequence"], optional: [] },
  request: { required: ["kind", "protocol", "version", "id", "method", "params"], optional: [] },
  response: { required: ["kind", "protocol", "version", "reply_to"], optional: ["result", "error"] },
  cancel: { required: ["kind", "protocol", "version", "id", "target_request_id"], optional: [] },
  subscribe: { required: ["kind", "protocol", "version", "id", "topic", "from_sequence"], optional: [] },
  subscribed: { required: ["kind", "protocol", "version", "reply_to", "subscription_id", "topic", "from_sequence", "replayed_through_sequence", "live_from_sequence", "minimum_available_sequence", "latest_sequence"], optional: [] },
  unsubscribe: { required: ["kind", "protocol", "version", "id", "subscription_id"], optional: [] },
  event: { required: ["kind", "protocol", "version", "subscription_id", "topic", "sequence", "event_type", "payload"], optional: [] },
  subscription_closed: { required: ["kind", "protocol", "version", "subscription_id", "code", "message", "resume_from_sequence"], optional: [] },
  protocol_error: { required: ["kind", "protocol", "version", "error"], optional: ["reply_to", "supported_versions"] },
  server_going_away: { required: ["kind", "protocol", "version", "reason", "reconnect_delay_milliseconds"], optional: [] }
});

export class ProtocolValidationError extends Error {
  constructor(message) {
    super(message);
    this.name = "ProtocolValidationError";
  }
}

export class RemoteProtocolError extends Error {
  constructor(error, supportedVersions = []) {
    super(error?.message ?? "Remote protocol error");
    this.name = "RemoteProtocolError";
    this.error = error;
    this.supportedVersions = [...supportedVersions];
  }
}

function isObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function assertObject(value, name) {
  if (!isObject(value)) throw new ProtocolValidationError(`${name} must be an object`);
}

function assertString(value, name, maximum = 256) {
  if (typeof value !== "string" || value.trim().length === 0 || value.length > maximum) {
    throw new ProtocolValidationError(`${name} must be a non-empty string of at most ${maximum} characters`);
  }
}

function assertInteger(value, name, minimum = 0) {
  if (!Number.isSafeInteger(value) || value < minimum) {
    throw new ProtocolValidationError(`${name} must be an integer >= ${minimum}`);
  }
}

function assertStringArray(value, name) {
  if (!Array.isArray(value)) throw new ProtocolValidationError(`${name} must be an array`);
  const seen = new Set();
  for (const item of value) {
    assertString(item, `${name} item`, 128);
    if (seen.has(item)) throw new ProtocolValidationError(`${name} contains duplicate '${item}'`);
    seen.add(item);
  }
}

function assertExactKeys(value, required, optional, name) {
  assertObject(value, name);
  const allowed = new Set([...required, ...optional]);
  for (const key of Object.keys(value)) {
    if (!allowed.has(key)) throw new ProtocolValidationError(`${name} contains unknown field '${key}'`);
  }
  for (const key of required) {
    if (!Object.hasOwn(value, key)) throw new ProtocolValidationError(`${name} is missing required field '${key}'`);
  }
}

function validateClient(value) {
  assertExactKeys(value, ["kind", "version", "instance_id"], [], "client");
  assertString(value.kind, "client.kind", 128);
  assertString(value.version, "client.version", 128);
  assertString(value.instance_id, "client.instance_id", 128);
}

function validateServer(value) {
  assertExactKeys(value, ["kind", "version", "instance_id", "epoch"], [], "server");
  assertString(value.kind, "server.kind", 128);
  assertString(value.version, "server.version", 128);
  assertString(value.instance_id, "server.instance_id", 128);
  assertInteger(value.epoch, "server.epoch", 1);
}

function validateLimits(value) {
  const fields = [
    "maximum_payload_bytes",
    "control_queue_capacity",
    "event_queue_capacity",
    "subscription_queue_capacity",
    "maximum_replay_events",
    "maximum_in_flight_requests",
    "write_timeout_milliseconds"
  ];
  assertExactKeys(value, fields, [], "limits");
  for (const field of fields) assertInteger(value[field], `limits.${field}`, 1);
}

function validateError(value) {
  assertExactKeys(value, ["code", "message"], ["data"], "error");
  assertString(value.code, "error.code", 128);
  assertString(value.message, "error.message", 4096);
}

export function validateProtocolMessage(message) {
  assertObject(message, "message");
  assertString(message.kind, "kind", 128);
  const schema = schemas[message.kind];
  if (!schema) throw new ProtocolValidationError(`unknown protocol message kind '${message.kind}'`);
  assertExactKeys(message, schema.required, schema.optional, message.kind);
  assertString(message.protocol, "protocol", 128);
  assertString(message.version, "version", 128);
  if (message.kind !== "hello") {
    if (message.protocol !== PROTOCOL_NAME) throw new ProtocolValidationError(`protocol must be '${PROTOCOL_NAME}'`);
    if (message.version !== CURRENT_VERSION) throw new ProtocolValidationError(`version must be '${CURRENT_VERSION}'`);
  }

  switch (message.kind) {
    case "hello":
      assertString(message.id, "id", 128);
      validateClient(message.client);
      assertStringArray(message.capabilities, "capabilities");
      break;
    case "hello_ack":
      assertString(message.reply_to, "reply_to", 128);
      validateServer(message.server);
      assertStringArray(message.capabilities, "capabilities");
      validateLimits(message.limits);
      assertInteger(message.minimum_available_sequence, "minimum_available_sequence", 1);
      assertInteger(message.latest_sequence, "latest_sequence", 0);
      break;
    case "request":
      assertString(message.id, "id", 128);
      assertString(message.method, "method", 128);
      assertObject(message.params, "params");
      break;
    case "response": {
      assertString(message.reply_to, "reply_to", 128);
      const hasResult = Object.hasOwn(message, "result");
      const hasError = Object.hasOwn(message, "error");
      if (hasResult === hasError) throw new ProtocolValidationError("response must contain exactly one of result or error");
      if (hasError) validateError(message.error);
      break;
    }
    case "cancel":
      assertString(message.id, "id", 128);
      assertString(message.target_request_id, "target_request_id", 128);
      break;
    case "subscribe":
      assertString(message.id, "id", 128);
      assertString(message.topic, "topic", 256);
      assertInteger(message.from_sequence, "from_sequence", 0);
      break;
    case "subscribed":
      for (const field of ["reply_to", "subscription_id", "topic"]) assertString(message[field], field, field === "topic" ? 256 : 128);
      assertInteger(message.from_sequence, "from_sequence", 0);
      assertInteger(message.replayed_through_sequence, "replayed_through_sequence", 0);
      assertInteger(message.live_from_sequence, "live_from_sequence", 1);
      assertInteger(message.minimum_available_sequence, "minimum_available_sequence", 1);
      assertInteger(message.latest_sequence, "latest_sequence", 0);
      break;
    case "unsubscribe":
      assertString(message.id, "id", 128);
      assertString(message.subscription_id, "subscription_id", 128);
      break;
    case "event":
      assertString(message.subscription_id, "subscription_id", 128);
      assertString(message.topic, "topic", 256);
      assertInteger(message.sequence, "sequence", 1);
      assertString(message.event_type, "event_type", 128);
      assertObject(message.payload, "payload");
      break;
    case "subscription_closed":
      assertString(message.subscription_id, "subscription_id", 128);
      assertString(message.code, "code", 128);
      assertString(message.message, "message", 4096);
      assertInteger(message.resume_from_sequence, "resume_from_sequence", 0);
      break;
    case "protocol_error":
      if (Object.hasOwn(message, "reply_to")) assertString(message.reply_to, "reply_to", 128);
      validateError(message.error);
      if (Object.hasOwn(message, "supported_versions")) assertStringArray(message.supported_versions, "supported_versions");
      break;
    case "server_going_away":
      assertString(message.reason, "reason", 512);
      assertInteger(message.reconnect_delay_milliseconds, "reconnect_delay_milliseconds", 0);
      break;
    default:
      throw new ProtocolValidationError(`unsupported kind '${message.kind}'`);
  }
  return message;
}

export function parseProtocolPayload(payload) {
  const text = decodeUtf8(payload);
  let message;
  try {
    message = JSON.parse(text);
  } catch (error) {
    throw new ProtocolValidationError(`protocol payload is not valid JSON: ${error.message}`);
  }
  return validateProtocolMessage(message);
}

export function serializeProtocolMessage(message) {
  validateProtocolMessage(message);
  return Buffer.from(JSON.stringify(message), "utf8");
}

export function makeMessage(kind, fields = {}, version = CURRENT_VERSION, protocol = PROTOCOL_NAME) {
  return { kind, protocol, version, ...fields };
}

export class EventSequenceTracker {
  #lastSequence;

  constructor(initialSequence = 0) {
    assertInteger(initialSequence, "initialSequence", 0);
    this.#lastSequence = initialSequence;
  }

  get lastSequence() {
    return this.#lastSequence;
  }

  observe(sequence) {
    assertInteger(sequence, "sequence", 1);
    const previousSequence = this.#lastSequence;
    if (sequence <= previousSequence) {
      return { previousSequence, currentSequence: sequence, isDuplicate: true, hasGap: false };
    }
    this.#lastSequence = sequence;
    return { previousSequence, currentSequence: sequence, isDuplicate: false, hasGap: sequence > previousSequence + 1 };
  }
}
