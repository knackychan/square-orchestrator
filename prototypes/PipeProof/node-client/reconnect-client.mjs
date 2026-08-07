#!/usr/bin/env node
import { NamedPipeProtocolClient, EventSequenceTracker } from "./named-pipe-client.mjs";
import { parseOptions, positiveInteger, required, withTimeout, writeJsonAtomic } from "./cli-utils.mjs";

const known = new Set([
  "--pipe-path",
  "--checkpoint",
  "--output",
  "--topic",
  "--before-count",
  "--after-count",
  "--timeout-ms"
]);

async function connect(pipePath, timeoutMilliseconds, connectionOrdinal) {
  const client = await NamedPipeProtocolClient.connect(pipePath, {
    clientKind: "node-reconnect-client",
    clientInstanceId: `node-reconnect-${process.pid}-${connectionOrdinal}`
  }, Math.min(timeoutMilliseconds, 5_000));
  try {
    await client.performHandshake();
    return client;
  } catch (error) {
    await client.close().catch(() => {});
    throw error;
  }
}

async function readCount(subscription, count, tracker, deadline) {
  const events = [];
  for (let index = 0; index < count; index += 1) {
    const remaining = deadline - Date.now();
    if (remaining <= 0) throw new Error("reconnect event read exceeded deadline");
    const item = await withTimeout(subscription.next(), remaining, "reconnect event read");
    if (item.done) throw new Error("reconnect subscription closed before expected events arrived");
    const observation = tracker.observe(item.value.sequence);
    if (observation.isDuplicate) throw new Error(`duplicate or regressed event sequence ${item.value.sequence}`);
    events.push(item.value);
  }
  return events;
}

async function main() {
  const values = parseOptions(process.argv.slice(2), known);
  const pipePath = required(values, "--pipe-path");
  const checkpointPath = required(values, "--checkpoint");
  const outputPath = required(values, "--output");
  const topic = required(values, "--topic");
  const beforeCount = positiveInteger(values, "--before-count");
  const afterCount = positiveInteger(values, "--after-count");
  const timeoutMilliseconds = positiveInteger(values, "--timeout-ms", 45_000);
  const deadline = Date.now() + timeoutMilliseconds;

  let connectionOrdinal = 1;
  const first = await connect(pipePath, timeoutMilliseconds, connectionOrdinal++);
  const firstServerInstanceId = first.handshake.server.instance_id;
  const tracker = new EventSequenceTracker();
  const firstSubscription = await first.subscribe(topic, 0);
  await first.request("proof.publish", {
    topic,
    count: beforeCount,
    payload_bytes: 0,
    label: "before-restart"
  });
  const beforeEvents = await readCount(firstSubscription, beforeCount, tracker, deadline);
  const lastSequence = tracker.lastSequence;
  await writeJsonAtomic(checkpointPath, {
    schema_version: "1.0",
    phase: "waiting_for_restart",
    process_id: process.pid,
    last_sequence: lastSequence,
    first_server_instance_id: firstServerInstanceId
  });

  await withTimeout(first.waitForClose(), Math.max(1, deadline - Date.now()), "first server disconnect");

  let secondServerInstanceId = null;
  let replayEvents = null;
  let successfulConnections = 1;
  let lastError = null;
  while (Date.now() < deadline && replayEvents === null) {
    let client;
    try {
      client = await connect(pipePath, Math.max(1, deadline - Date.now()), connectionOrdinal++);
      successfulConnections += 1;
      secondServerInstanceId = client.handshake.server.instance_id;
      const subscription = await client.subscribe(topic, tracker.lastSequence);
      replayEvents = await readCount(subscription, afterCount, tracker, deadline);
      await subscription.dispose();
      await client.close();
    } catch (error) {
      lastError = error;
      if (client) await client.close().catch(() => {});
      await new Promise(resolve => setTimeout(resolve, 100));
    }
  }
  if (replayEvents === null) throw lastError ?? new Error("node reconnect did not complete");

  const result = {
    schema_version: "1.0",
    scenario: "node_restart_reconnect",
    client: "node",
    first_server_instance_id: firstServerInstanceId,
    second_server_instance_id: secondServerInstanceId,
    server_instance_changed: firstServerInstanceId !== secondServerInstanceId,
    before_sequences: beforeEvents.map(event => event.sequence),
    replay_sequences: replayEvents.map(event => event.sequence),
    replay_labels: replayEvents.map(event => event.payload.label),
    final_sequence: tracker.lastSequence,
    reconnected: successfulConnections >= 2,
    successful_connections: successfulConnections
  };
  await writeJsonAtomic(outputPath, result);
  process.stdout.write(`${JSON.stringify(result)}\n`);
}

main().catch(error => {
  console.error(error?.stack ?? String(error));
  process.exitCode = 1;
});
