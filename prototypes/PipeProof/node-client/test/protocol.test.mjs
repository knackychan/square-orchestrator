import test from "node:test";
import assert from "node:assert/strict";
import { readdir, readFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import {
  EventSequenceTracker,
  ProtocolValidationError,
  parseProtocolPayload,
  validateProtocolMessage
} from "../protocol.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const fixtures = resolve(here, "../../fixtures/contracts");
const protocolVectors = resolve(here, "../../protocol-vectors.json");

test("all golden contract fixtures validate", async () => {
  const names = (await readdir(fixtures)).filter(name => name.endsWith(".json")).sort();
  assert.equal(names.length, 13);
  for (const name of names) {
    const bytes = await readFile(join(fixtures, name));
    assert.doesNotThrow(() => parseProtocolPayload(bytes), name);
  }
});

test("shared .NET and Node protocol vectors agree", async () => {
  const vectors = JSON.parse(await readFile(protocolVectors, "utf8"));
  assert.equal(vectors.schema_version, "1.0");
  assert.equal(vectors.valid_messages.length, 13);
  assert.ok(vectors.invalid_messages.length >= 10);
  for (const vector of vectors.valid_messages) {
    assert.doesNotThrow(() => parseProtocolPayload(Buffer.from(vector.json, "utf8")), vector.name);
  }
  for (const vector of vectors.invalid_messages) {
    assert.throws(
      () => parseProtocolPayload(Buffer.from(vector.json, "utf8")),
      ProtocolValidationError,
      vector.name
    );
  }
});

test("malformed JSON is rejected", () => {
  assert.throws(() => parseProtocolPayload(Buffer.from("{not-json}")), ProtocolValidationError);
});

test("unknown fields are rejected", () => {
  const message = {
    kind: "request",
    protocol: "square.rpc",
    version: "1.0",
    id: "r1",
    method: "proof.echo",
    params: {},
    unexpected: true
  };
  assert.throws(() => validateProtocolMessage(message), /unknown field 'unexpected'/);
});

test("response requires exactly one result or error", () => {
  const base = { kind: "response", protocol: "square.rpc", version: "1.0", reply_to: "r1" };
  assert.throws(() => validateProtocolMessage(base), /exactly one/);
  assert.throws(() => validateProtocolMessage({ ...base, result: {}, error: { code: "x", message: "x" } }), /exactly one/);
  assert.doesNotThrow(() => validateProtocolMessage({ ...base, result: {} }));
});

test("sequence tracker reports gaps and regressions", () => {
  const tracker = new EventSequenceTracker();
  assert.deepEqual(tracker.observe(1), { previousSequence: 0, currentSequence: 1, isDuplicate: false, hasGap: false });
  assert.equal(tracker.observe(3).hasGap, true);
  assert.equal(tracker.observe(2).isDuplicate, true);
  assert.equal(tracker.lastSequence, 3);
});
