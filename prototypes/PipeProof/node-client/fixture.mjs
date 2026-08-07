#!/usr/bin/env node
import { randomUUID } from "node:crypto";
import { NamedPipeProtocolClient } from "./named-pipe-client.mjs";
import { CURRENT_VERSION, RemoteProtocolError } from "./protocol.mjs";
import { parseOptions, positiveInteger, required, withTimeout, writeJsonAtomic } from "./cli-utils.mjs";

const known = new Set([
  "--pipe-name",
  "--scenario",
  "--output",
  "--write-fragment-bytes",
  "--timeout-ms",
  "--topic",
  "--from-sequence",
  "--event-count",
  "--ready-file",
  "--progress-file"
]);

function pipePath(pipeName) {
  if (typeof pipeName !== "string" || pipeName.length === 0 || pipeName.includes("\\") || pipeName.includes("/")) {
    throw new Error("--pipe-name must be a local pipe name without path separators");
  }
  return `\\\\.\\pipe\\${pipeName}`;
}

function nonNegativeInteger(values, name, fallback = 0) {
  const text = values.get(name);
  if (text === undefined) return fallback;
  const value = Number(text);
  if (!Number.isSafeInteger(value) || value < 0) {
    throw new Error(`option '${name}' must be a non-negative safe integer`);
  }
  return value;
}

function strictlyIncreasing(values) {
  return values.every((value, index) => index === 0 || value > values[index - 1]);
}

function queuesWithinDeclaredBounds(stats, limits) {
  if (stats.maximum_observed_queue_depth > limits.control_queue_capacity + limits.event_queue_capacity) return false;
  if (stats.maximum_observed_subscription_queue_depth > limits.subscription_queue_capacity) return false;
  return stats.connections.every(connection =>
    connection.queue.peak_total_depth
      <= connection.queue.control_capacity + connection.queue.event_capacity);
}

async function connect(path, clientKind, timeoutMilliseconds, options = {}) {
  const client = await NamedPipeProtocolClient.connect(path, {
    clientKind,
    clientInstanceId: `${clientKind}-${process.pid}-${randomUUID()}`,
    ...options
  }, Math.min(timeoutMilliseconds, 10_000));
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
    if (item.done) throw new Error("subscription closed before all expected events arrived");
    events.push(item.value);
  }
  return events;
}

async function runParity(path, writeFragmentBytes, timeoutMilliseconds) {
  const client = await connect(path, "node-proof-client", timeoutMilliseconds, {
    maximumWriteChunkBytes: writeFragmentBytes
  });
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
      const stats = await client.request("proof.stats", {});
      const limits = client.handshake.limits;
      return {
        schema_version: "1.0",
        scenario: "parity",
        client: "node",
        protocol: client.handshake.protocol,
        version: client.handshake.version,
        echo_text: echo.text,
        cancel_acknowledged: cancel.error === undefined && cancel.result?.acknowledged === true,
        cancellation_code: cancelled.error.code,
        published_count: published.count,
        labels: events.map(event => event.payload.label),
        ordinals: events.map(event => event.payload.ordinal),
        event_sequences_strictly_increasing: strictlyIncreasing(events.map(event => event.sequence)),
        declared_control_queue_capacity: limits.control_queue_capacity,
        declared_event_queue_capacity: limits.event_queue_capacity,
        declared_subscription_queue_capacity: limits.subscription_queue_capacity,
        server_queues_within_declared_bounds: queuesWithinDeclaredBounds(stats, limits)
      };
    } finally {
      await subscription.dispose();
    }
  } finally {
    await client.close();
  }
}

async function runIncompatible(path, timeoutMilliseconds) {
  const client = await NamedPipeProtocolClient.connect(path, {
    clientKind: "node-incompatible-client",
    clientInstanceId: `node-incompatible-${process.pid}-${randomUUID()}`,
    requestedVersion: "9.9"
  }, Math.min(timeoutMilliseconds, 10_000));
  try {
    await client.performHandshake();
    throw new Error("incompatible protocol handshake unexpectedly succeeded");
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

async function runReplay(path, topic, fromSequence, eventCount, timeoutMilliseconds) {
  const client = await connect(path, "node-replay-client", timeoutMilliseconds);
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

async function runReconnect(path, topic, eventCount, readyFile, progressFile, timeoutMilliseconds) {
  if (!readyFile || !progressFile) {
    throw new Error("reconnect requires --ready-file and --progress-file");
  }

  const deadline = Date.now() + timeoutMilliseconds;
  const sequences = [];
  const ordinals = [];
  let lastSequence = 0;
  let successfulConnections = 0;
  let readyWritten = false;
  let lastError = null;

  while (sequences.length < eventCount && Date.now() < deadline) {
    let client;
    let subscription;
    try {
      client = await connect(path, "node-reconnect-client", Math.max(1, deadline - Date.now()));
      successfulConnections += 1;
      subscription = await client.subscribe(topic, lastSequence);
      if (!readyWritten) {
        await writeJsonAtomic(readyFile, {
          schema_version: "1.0",
          ready: true,
          process_id: process.pid,
          successful_connections: successfulConnections,
          last_sequence: lastSequence
        });
        readyWritten = true;
      }

      while (sequences.length < eventCount) {
        const remaining = deadline - Date.now();
        if (remaining <= 0) throw new Error("reconnect event read exceeded deadline");
        const item = await withTimeout(subscription.next(), remaining, "reconnect event read");
        if (item.done) throw new Error("reconnect subscription closed before completion");
        const event = item.value;
        if (event.sequence <= lastSequence) {
          throw new Error(`reconnect observed duplicate or regressing sequence ${event.sequence}`);
        }
        lastSequence = event.sequence;
        sequences.push(event.sequence);
        ordinals.push(event.payload.ordinal);
        await writeJsonAtomic(progressFile, {
          schema_version: "1.0",
          count: sequences.length,
          sequences,
          ordinals,
          successful_connections: successfulConnections
        });
      }
    } catch (error) {
      lastError = error;
      if (Date.now() >= deadline) break;
      await new Promise(resolve => setTimeout(resolve, 100));
    } finally {
      if (subscription) await subscription.dispose().catch(() => {});
      if (client) await client.close().catch(() => {});
    }
  }

  if (sequences.length !== eventCount) {
    throw lastError ?? new Error(`reconnect received ${sequences.length}/${eventCount} expected events`);
  }
  if (!strictlyIncreasing(sequences)) throw new Error("reconnect sequences are not strictly increasing");

  return {
    schema_version: "1.0",
    scenario: "reconnect",
    client: "node",
    sequences,
    ordinals,
    successful_connections: successfulConnections,
    last_sequence: lastSequence
  };
}

async function main() {
  const values = parseOptions(process.argv.slice(2), known);
  const path = pipePath(required(values, "--pipe-name"));
  const scenario = required(values, "--scenario");
  const writeFragmentBytes = positiveInteger(values, "--write-fragment-bytes", Number.MAX_SAFE_INTEGER);
  const timeoutMilliseconds = positiveInteger(values, "--timeout-ms", scenario === "reconnect" ? 45_000 : 20_000);
  const topic = values.get("--topic") ?? "reconnect";
  const fromSequence = nonNegativeInteger(values, "--from-sequence", 0);
  const eventCount = positiveInteger(values, "--event-count", 1);

  let result;
  switch (scenario) {
    case "parity":
      result = await runParity(path, writeFragmentBytes, timeoutMilliseconds);
      break;
    case "incompatible":
      result = await runIncompatible(path, timeoutMilliseconds);
      break;
    case "replay":
      result = await runReplay(path, topic, fromSequence, eventCount, timeoutMilliseconds);
      break;
    case "reconnect":
      result = await runReconnect(
        path,
        topic,
        eventCount,
        values.get("--ready-file"),
        values.get("--progress-file"),
        timeoutMilliseconds);
      break;
    default:
      throw new Error(`unsupported scenario '${scenario}'`);
  }

  if (result.version && result.version !== CURRENT_VERSION) {
    throw new Error("server returned the wrong current protocol version");
  }
  const output = values.get("--output");
  if (output) await writeJsonAtomic(output, result);
  process.stdout.write(`${JSON.stringify(result)}\n`);
}

main().catch(error => {
  console.error(error?.stack ?? String(error));
  process.exitCode = 1;
});
