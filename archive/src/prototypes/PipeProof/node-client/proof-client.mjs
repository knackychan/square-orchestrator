#!/usr/bin/env node
import { randomUUID } from "node:crypto";
import { NamedPipeProtocolClient } from "./named-pipe-client.mjs";
import { CURRENT_VERSION, RemoteProtocolError } from "./protocol.mjs";
import { parseOptions, positiveInteger, required, withTimeout, writeJsonAtomic } from "./cli-utils.mjs";

const known = new Set([
  "--pipe-path",
  "--scenario",
  "--output",
  "--write-fragment-bytes",
  "--timeout-ms",
  "--topic",
  "--from-sequence",
  "--event-count"
]);

async function connect(pipePath, options, timeoutMilliseconds) {
  const client = await NamedPipeProtocolClient.connect(pipePath, options, timeoutMilliseconds);
  try {
    await client.performHandshake();
    return client;
  } catch (error) {
    await client.close().catch(() => {});
    throw error;
  }
}

async function readEvents(subscription, count, timeoutMilliseconds) {
  const events = [];
  for (let index = 0; index < count; index += 1) {
    const item = await withTimeout(subscription.next(), timeoutMilliseconds, "event read");
    if (item.done) throw new Error("subscription closed before all events arrived");
    events.push(item.value);
  }
  return events;
}

function strictlyIncreasing(values) {
  return values.every((value, index) => index === 0 || value > values[index - 1]);
}

async function parity(pipePath, writeFragmentBytes, timeoutMilliseconds) {
  const client = await connect(pipePath, {
    clientKind: "node-proof-client",
    maximumWriteChunkBytes: writeFragmentBytes
  }, timeoutMilliseconds);
  try {
    const echo = await client.request("proof.echo", { text: "parity café 東京 🙂" });
    const delayed = client.beginRequest("proof.delay", { milliseconds: 10_000 });
    await new Promise(resolve => setTimeout(resolve, 100));
    const cancel = await client.cancel(delayed.id);
    const cancelled = await delayed.response;
    if (!cancelled.error) throw new Error("cancelled request did not return a typed error");

    const topic = `parity-node-${randomUUID().replaceAll("-", "")}`;
    const subscription = await client.subscribe(topic, 0);
    try {
      const published = await client.request("proof.publish", {
        topic,
        count: 3,
        payload_bytes: 0,
        label: "parity"
      });
      const events = await readEvents(subscription, 3, timeoutMilliseconds);
      const sequences = events.map(event => event.sequence);
      const stats = await client.request("proof.stats", {});
      const limits = client.handshake.limits;
      const queuesWithinBounds =
        stats.maximum_observed_queue_depth <= limits.control_queue_capacity + limits.event_queue_capacity
        && stats.maximum_observed_subscription_queue_depth <= limits.subscription_queue_capacity;
      return {
        schema_version: "1.0",
        scenario: "parity",
        client: "node",
        protocol: client.handshake.protocol,
        version: client.handshake.version,
        server_instance_id: client.handshake.server.instance_id,
        echo_text: echo.text,
        cancel_acknowledged: cancel.error === undefined && cancel.result?.acknowledged === true,
        cancellation_code: cancelled.error.code,
        published_count: published.count,
        event_labels: events.map(event => event.payload.label),
        event_ordinals: events.map(event => event.payload.ordinal),
        event_sequences: sequences,
        event_sequences_strictly_increasing: strictlyIncreasing(sequences),
        declared_control_queue_capacity: limits.control_queue_capacity,
        declared_event_queue_capacity: limits.event_queue_capacity,
        declared_subscription_queue_capacity: limits.subscription_queue_capacity,
        server_queues_within_declared_bounds: queuesWithinBounds
      };
    } finally {
      await subscription.dispose();
    }
  } finally {
    await client.close();
  }
}

async function incompatible(pipePath, timeoutMilliseconds) {
  const client = await NamedPipeProtocolClient.connect(pipePath, {
    clientKind: "node-incompatible-client",
    requestedVersion: "9.9"
  }, timeoutMilliseconds);
  try {
    await client.performHandshake();
    throw new Error("incompatible handshake unexpectedly succeeded");
  } catch (error) {
    if (!(error instanceof RemoteProtocolError)) throw error;
    return {
      schema_version: "1.0",
      scenario: "incompatible",
      client: "node",
      error_code: error.error.code,
      supported_versions: error.supportedVersions
    };
  } finally {
    await client.close().catch(() => {});
  }
}

async function replay(pipePath, topic, fromSequence, eventCount, timeoutMilliseconds) {
  const client = await connect(pipePath, { clientKind: "node-replay-client" }, timeoutMilliseconds);
  try {
    const subscription = await client.subscribe(topic, fromSequence);
    try {
      const events = await readEvents(subscription, eventCount, timeoutMilliseconds);
      return {
        schema_version: "1.0",
        scenario: "replay",
        client: "node",
        requested_from_sequence: fromSequence,
        sequences: events.map(event => event.sequence),
        ordinals: events.map(event => event.payload.ordinal),
        latest_sequence_at_subscribe: subscription.accepted.latest_sequence
      };
    } finally {
      await subscription.dispose();
    }
  } finally {
    await client.close();
  }
}

async function main() {
  const values = parseOptions(process.argv.slice(2), known);
  const pipePath = required(values, "--pipe-path");
  const scenario = required(values, "--scenario");
  const output = required(values, "--output");
  const writeFragmentBytes = positiveInteger(values, "--write-fragment-bytes", Number.MAX_SAFE_INTEGER);
  const timeoutMilliseconds = positiveInteger(values, "--timeout-ms", 20_000);
  const topic = values.get("--topic") ?? "reconnect";
  const fromSequence = Number(values.get("--from-sequence") ?? "0");
  const eventCount = positiveInteger(values, "--event-count", 1);
  if (!Number.isSafeInteger(fromSequence) || fromSequence < 0) {
    throw new Error("option '--from-sequence' must be a non-negative safe integer");
  }

  let result;
  switch (scenario) {
    case "parity":
      result = await parity(pipePath, writeFragmentBytes, timeoutMilliseconds);
      break;
    case "incompatible":
      result = await incompatible(pipePath, timeoutMilliseconds);
      break;
    case "replay":
      result = await replay(pipePath, topic, fromSequence, eventCount, timeoutMilliseconds);
      break;
    default:
      throw new Error(`unsupported scenario '${scenario}'`);
  }
  if (result.version && result.version !== CURRENT_VERSION) {
    throw new Error("server returned the wrong current version");
  }
  await writeJsonAtomic(output, result);
  process.stdout.write(`${JSON.stringify(result)}\n`);
}

main().catch(error => {
  console.error(error?.stack ?? String(error));
  process.exitCode = 1;
});
