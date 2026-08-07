import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { createSyntheticFrames, validateFixtureAccessibility } from "../dist/src/shared/fixture.js";
import { parseFixture, sha256Hex } from "../dist/src/shared/protocol.js";

const fixture = parseFixture(JSON.parse(await readFile(new URL("../fixtures/canonical-state.json", import.meta.url), "utf8")));
const expectedHash = (await readFile(new URL("../fixtures/canonical-state.sha256", import.meta.url), "utf8")).trim();

test("canonical fixture has eight unique, fully labelled terminals", async () => {
  assert.equal(fixture.terminals.length, 8);
  assert.equal(new Set(fixture.terminals.map((terminal) => terminal.id)).size, 8);
  assert.deepEqual(validateFixtureAccessibility(fixture), []);
  assert.equal(await sha256Hex(fixture), expectedHash);
});

test("synthetic output is deterministic, bounded, Unicode, and ANSI-bearing", () => {
  const terminal = fixture.terminals[0];
  assert.ok(terminal);
  const first = createSyntheticFrames(terminal, 32_768, 4_096);
  const second = createSyntheticFrames(terminal, 32_768, 4_096);
  assert.equal(first.length, 8);
  assert.equal(first.reduce((total, frame) => total + frame.bytes.byteLength, 0), 32_768);
  assert.deepEqual(first.map((frame) => frame.sequence), second.map((frame) => frame.sequence));
  assert.deepEqual(first[0]?.bytes, second[0]?.bytes);
  const sample = new TextDecoder().decode(first[0]?.bytes);
  assert.match(sample, /\u001b\[36m/);
  assert.match(sample, /測試/);
});
