import net from "node:net";
import { randomUUID } from "node:crypto";
import { once } from "node:events";
import {
  CURRENT_VERSION,
  PROTOCOL_NAME,
  EventSequenceTracker,
  RemoteProtocolError,
  makeMessage,
  parseProtocolPayload,
  serializeProtocolMessage
} from "./protocol.mjs";
import { DEFAULT_MAXIMUM_PAYLOAD_BYTES, FrameDecoder, encodeFrame } from "./frame-codec.mjs";

class AsyncMessageQueue {
  #capacity;
  #values = [];
  #waiters = [];
  #closed = false;
  #error = null;

  constructor(capacity) {
    if (!Number.isSafeInteger(capacity) || capacity <= 0) throw new RangeError("queue capacity must be positive");
    this.#capacity = capacity;
  }

  push(value) {
    if (this.#closed) return false;
    const waiter = this.#waiters.shift();
    if (waiter) {
      waiter.resolve({ value, done: false });
      return true;
    }
    if (this.#values.length >= this.#capacity) return false;
    this.#values.push(value);
    return true;
  }

  close(error = null) {
    if (this.#closed) return;
    this.#closed = true;
    this.#error = error;
    for (const waiter of this.#waiters.splice(0)) {
      if (error) waiter.reject(error);
      else waiter.resolve({ value: undefined, done: true });
    }
  }

  async next() {
    if (this.#values.length > 0) return { value: this.#values.shift(), done: false };
    if (this.#closed) {
      if (this.#error) throw this.#error;
      return { value: undefined, done: true };
    }
    return new Promise((resolve, reject) => this.#waiters.push({ resolve, reject }));
  }
}

export class ProtocolSubscription {
  #connection;
  #queue;
  #disposed = false;

  constructor(connection, accepted, capacity) {
    this.#connection = connection;
    this.accepted = accepted;
    this.#queue = new AsyncMessageQueue(capacity);
  }

  get id() { return this.accepted.subscription_id; }
  get topic() { return this.accepted.topic; }

  push(message) { return this.#queue.push(message); }
  close(error = null) { this.#queue.close(error); }
  next() { return this.#queue.next(); }
  [Symbol.asyncIterator]() { return this; }

  async dispose() {
    if (this.#disposed) return;
    this.#disposed = true;
    try {
      await this.#connection.unsubscribe(this.id);
    } catch {
      // The connection may already be gone; local closure is still deterministic.
    }
    this.close();
  }
}

export class NamedPipeProtocolClient {
  #socket;
  #options;
  #decoder;
  #pending = new Map();
  #subscriptions = new Map();
  #closed = false;
  #closeError = null;
  #closedResolve;
  #closedPromise;
  #writeChain = Promise.resolve();

  constructor(socket, options = {}) {
    this.#socket = socket;
    this.#options = {
      clientKind: options.clientKind ?? "node-proof-client",
      clientVersion: options.clientVersion ?? "0.1.0",
      clientInstanceId: options.clientInstanceId ?? `node-${randomUUID()}`,
      requestedProtocol: options.requestedProtocol ?? PROTOCOL_NAME,
      requestedVersion: options.requestedVersion ?? CURRENT_VERSION,
      maximumPayloadBytes: options.maximumPayloadBytes ?? DEFAULT_MAXIMUM_PAYLOAD_BYTES,
      maximumWriteChunkBytes: options.maximumWriteChunkBytes ?? Number.MAX_SAFE_INTEGER,
      localSubscriptionCapacity: options.localSubscriptionCapacity ?? 256
    };
    this.#decoder = new FrameDecoder(this.#options.maximumPayloadBytes);
    this.#closedPromise = new Promise(resolve => { this.#closedResolve = resolve; });
    socket.on("data", chunk => this.#onData(chunk));
    socket.on("error", error => { this.#closeError = error; });
    socket.on("close", () => this.#onClose());
  }

  static async connect(pipePath, options = {}, timeoutMilliseconds = 10_000) {
    if (typeof pipePath !== "string" || pipePath.length === 0) throw new TypeError("pipePath is required");
    if (!Number.isSafeInteger(timeoutMilliseconds) || timeoutMilliseconds <= 0) {
      throw new RangeError("timeoutMilliseconds must be a positive safe integer");
    }
    const socket = net.createConnection(pipePath);
    await new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        cleanup();
        const error = new Error(`pipe connection timed out after ${timeoutMilliseconds} ms`);
        socket.destroy(error);
        reject(error);
      }, timeoutMilliseconds);
      const onConnect = () => {
        cleanup();
        resolve();
      };
      const onError = error => {
        cleanup();
        reject(error);
      };
      const cleanup = () => {
        clearTimeout(timeout);
        socket.off("connect", onConnect);
        socket.off("error", onError);
      };
      socket.once("connect", onConnect);
      socket.once("error", onError);
    });
    socket.setNoDelay(true);
    return new NamedPipeProtocolClient(socket, options);
  }

  handshake = null;

  async performHandshake() {
    if (this.handshake) throw new Error("handshake has already completed");
    const id = this.#newId("hello");
    const pending = this.#registerPending(id, "hello");
    await this.#send(makeMessage("hello", {
      id,
      client: {
        kind: this.#options.clientKind,
        version: this.#options.clientVersion,
        instance_id: this.#options.clientInstanceId
      },
      capabilities: ["request", "cancel", "subscribe", "replay"]
    }, this.#options.requestedVersion, this.#options.requestedProtocol));
    const accepted = await pending;
    this.handshake = accepted;
    return accepted;
  }

  beginRequest(method, params = {}) {
    this.#ensureReady();
    const id = this.#newId("request");
    const response = this.#registerPending(id, "request");
    this.#send(makeMessage("request", { id, method, params })).catch(error => this.#rejectPending(id, error));
    return { id, response };
  }

  async request(method, params = {}) {
    const pending = this.beginRequest(method, params);
    const response = await pending.response;
    if (response.error) throw new RemoteProtocolError(response.error);
    return response.result;
  }

  async cancel(targetRequestId) {
    this.#ensureReady();
    const id = this.#newId("cancel");
    const response = this.#registerPending(id, "request");
    await this.#send(makeMessage("cancel", { id, target_request_id: targetRequestId }));
    return response;
  }

  async subscribe(topic, fromSequence = 0) {
    this.#ensureReady();
    const id = this.#newId("subscribe");
    const accepted = this.#registerPending(id, "subscribe");
    await this.#send(makeMessage("subscribe", { id, topic, from_sequence: fromSequence }));
    return accepted;
  }

  async unsubscribe(subscriptionId) {
    if (this.#closed) return;
    this.#ensureReady();
    const id = this.#newId("unsubscribe");
    const responsePromise = this.#registerPending(id, "request");
    await this.#send(makeMessage("unsubscribe", { id, subscription_id: subscriptionId }));
    const response = await responsePromise;
    if (response.error) throw new RemoteProtocolError(response.error);
    const subscription = this.#subscriptions.get(subscriptionId);
    if (subscription) {
      this.#subscriptions.delete(subscriptionId);
      subscription.close();
    }
  }

  waitForClose() {
    return this.#closedPromise;
  }

  async close() {
    if (this.#closed) return;
    this.#socket.end();
    const timeout = setTimeout(() => this.#socket.destroy(), 1_000);
    try {
      await this.#closedPromise;
    } finally {
      clearTimeout(timeout);
    }
  }

  #onData(chunk) {
    try {
      for (const payload of this.#decoder.push(chunk)) {
        this.#dispatch(parseProtocolPayload(payload));
      }
    } catch (error) {
      this.#closeError = error;
      this.#socket.destroy(error);
    }
  }

  #dispatch(message) {
    switch (message.kind) {
      case "hello_ack":
        this.#resolvePending(message.reply_to, message, "hello");
        break;
      case "response":
        this.#resolvePending(message.reply_to, message, "request");
        break;
      case "subscribed": {
        const pending = this.#pending.get(message.reply_to);
        if (!pending || pending.kind !== "subscribe") throw new Error(`subscription reply '${message.reply_to}' is unknown`);
        this.#pending.delete(message.reply_to);
        const subscription = new ProtocolSubscription(this, message, this.#options.localSubscriptionCapacity);
        if (this.#subscriptions.has(subscription.id)) throw new Error(`duplicate subscription '${subscription.id}'`);
        this.#subscriptions.set(subscription.id, subscription);
        pending.resolve(subscription);
        break;
      }
      case "protocol_error": {
        const error = new RemoteProtocolError(message.error, message.supported_versions ?? []);
        if (message.reply_to) this.#rejectPending(message.reply_to, error);
        else throw error;
        break;
      }
      case "event": {
        const subscription = this.#subscriptions.get(message.subscription_id);
        if (!subscription) throw new Error(`event references unknown subscription '${message.subscription_id}'`);
        if (!subscription.push(message)) throw new Error(`local subscription '${message.subscription_id}' exceeded its bounded capacity`);
        break;
      }
      case "subscription_closed": {
        const subscription = this.#subscriptions.get(message.subscription_id);
        if (subscription) {
          this.#subscriptions.delete(message.subscription_id);
          subscription.close(new RemoteProtocolError({ code: message.code, message: message.message, data: { resume_from_sequence: message.resume_from_sequence } }));
        }
        break;
      }
      case "server_going_away":
        this.#closeError = new Error(`server going away: ${message.reason}`);
        this.#socket.destroy();
        break;
      default:
        throw new Error(`unexpected inbound message '${message.kind}'`);
    }
  }

  #onClose() {
    if (this.#closed) return;
    this.#closed = true;
    try { this.#decoder.finish(); } catch (error) { this.#closeError ??= error; }
    const error = this.#closeError ?? new Error("protocol connection closed");
    for (const { reject } of this.#pending.values()) reject(error);
    this.#pending.clear();
    for (const subscription of this.#subscriptions.values()) subscription.close(error);
    this.#subscriptions.clear();
    this.#closedResolve({ error });
  }

  #registerPending(id, kind) {
    if (this.#pending.has(id)) throw new Error(`duplicate pending id '${id}'`);
    return new Promise((resolve, reject) => this.#pending.set(id, { kind, resolve, reject }));
  }

  #resolvePending(id, value, expectedKind) {
    const pending = this.#pending.get(id);
    if (!pending || pending.kind !== expectedKind) throw new Error(`reply '${id}' is unknown or has the wrong kind`);
    this.#pending.delete(id);
    pending.resolve(value);
  }

  #rejectPending(id, error) {
    const pending = this.#pending.get(id);
    if (!pending) return false;
    this.#pending.delete(id);
    pending.reject(error);
    return true;
  }

  async #send(message) {
    if (this.#closed) throw this.#closeError ?? new Error("protocol connection is closed");
    const frame = encodeFrame(serializeProtocolMessage(message), this.#options.maximumPayloadBytes);
    this.#writeChain = this.#writeChain.then(async () => {
      const chunkSize = Math.min(this.#options.maximumWriteChunkBytes, frame.length);
      for (let offset = 0; offset < frame.length; offset += chunkSize) {
        const chunk = frame.subarray(offset, Math.min(offset + chunkSize, frame.length));
        if (!this.#socket.write(chunk)) await once(this.#socket, "drain");
      }
    });
    return this.#writeChain;
  }

  #ensureReady() {
    if (!this.handshake) throw new Error("protocol handshake has not completed");
    if (this.#closed) throw this.#closeError ?? new Error("protocol connection is closed");
  }

  #newId(prefix) {
    return `${prefix}-${randomUUID().replaceAll("-", "")}`;
  }
}

export { EventSequenceTracker };
