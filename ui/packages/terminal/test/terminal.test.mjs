import test from "node:test";
import assert from "node:assert/strict";
import { applyTerminalFrame, createTerminalStreamState } from "../dist/index.js";

const bytes = (text) => new TextEncoder().encode(text);

test("frames apply in order and duplicates are idempotent", () => {
  const first = applyTerminalFrame(createTerminalStreamState(), { sequence: 1, bytes: bytes("a") });
  const duplicate = applyTerminalFrame(first, { sequence: 1, bytes: bytes("a") });
  assert.strictEqual(duplicate, first);
  assert.equal(first.lastSequence, 1);
});

test("unexplained sequence gaps are explicit and do not invent data", () => {
  const state = applyTerminalFrame(createTerminalStreamState(), { sequence: 2, bytes: bytes("b") });
  assert.deepEqual(state.gap, { expected: 1, received: 2 });
  assert.equal(state.frames.length, 0);
});

test("a server truncation marker permits a bounded replay snapshot", () => {
  const state = applyTerminalFrame(createTerminalStreamState(), { sequence: 100, truncatedBefore: 99, bytes: bytes("snapshot") });
  assert.equal(state.lastSequence, 100);
  assert.equal(state.truncatedBefore, 99);
  assert.equal(state.gap, null);
});

test("retention is bounded and records truncation", () => {
  let state = createTerminalStreamState();
  state = applyTerminalFrame(state, { sequence: 1, bytes: bytes("1234") }, 5);
  state = applyTerminalFrame(state, { sequence: 2, bytes: bytes("5678") }, 5);
  assert.equal(state.frames.length, 1);
  assert.equal(state.truncatedBefore, 1);
  assert.ok(state.retainedBytes <= 5);
});

test("invalid sequence and truncation metadata fail closed", () => {
  assert.throws(() => applyTerminalFrame(createTerminalStreamState(), { sequence: 0, bytes: bytes("x") }), /sequence/);
  assert.throws(() => applyTerminalFrame(createTerminalStreamState(), { sequence: 1, truncatedBefore: 1, bytes: bytes("x") }), /truncatedBefore/);
});
