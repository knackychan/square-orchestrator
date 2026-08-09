import test from "node:test";
import assert from "node:assert/strict";
import { TerminalRenderQueue } from "../dist/src/web/render-queue.js";

class FakeTimer {
  work = [];
  schedule(callback, delayMs) {
    const item = { callback, delayMs, cancelled: false };
    this.work.push(item);
    return { cancel: () => { item.cancelled = true; } };
  }
  runNext() {
    const item = this.work.shift();
    if (item && !item.cancelled) item.callback();
    return item;
  }
}

function frame(sequence, text) {
  return { sequence, bytes: new TextEncoder().encode(text) };
}

test("visible frames batch immediately and preserve sequence", () => {
  const timer = new FakeTimer();
  const writes = [];
  const queue = new TerminalRenderQueue(
    { write: (bytes, done) => { writes.push(new TextDecoder().decode(bytes)); done(); } },
    timer,
    { maximumPendingBytes: 1024, hiddenDelayMs: 250 }
  );
  assert.equal(queue.enqueue(frame(1, "a")).kind, "accepted");
  assert.equal(queue.enqueue(frame(2, "b")).kind, "accepted");
  assert.equal(timer.work[0].delayMs, 0);
  timer.runNext();
  assert.deepEqual(writes, ["ab"]);
  assert.equal(queue.snapshot().renderedThroughSequence, 2);
});

test("hidden panes delay presentation without losing frames", () => {
  const timer = new FakeTimer();
  const writes = [];
  const queue = new TerminalRenderQueue(
    { write: (bytes, done) => { writes.push(new TextDecoder().decode(bytes)); done(); } },
    timer,
    { maximumPendingBytes: 1024, hiddenDelayMs: 500 }
  );
  queue.setVisible(false);
  queue.enqueue(frame(1, "hidden"));
  assert.equal(timer.work.at(-1).delayMs, 500);
  assert.equal(queue.snapshot().pendingFrames, 1);
  queue.setVisible(true);
  assert.equal(timer.work.at(-1).delayMs, 0);
  timer.runNext();
  timer.runNext();
  assert.deepEqual(writes, ["hidden"]);
  assert.equal(queue.snapshot().renderedThroughSequence, 1);
});

test("gaps and overflow are explicit and do not advance accepted sequence", () => {
  const timer = new FakeTimer();
  const queue = new TerminalRenderQueue(
    { write: (_bytes, done) => done() },
    timer,
    { maximumPendingBytes: 3, hiddenDelayMs: 250 }
  );
  assert.deepEqual(queue.enqueue(frame(2, "x")), { kind: "gap", expected: 1, received: 2 });
  assert.equal(queue.enqueue(frame(1, "abc")).kind, "accepted");
  assert.deepEqual(queue.enqueue(frame(2, "d")), { kind: "overflow", pendingBytes: 3, limitBytes: 3 });
  assert.equal(queue.snapshot().lastSequence, 1);
});
