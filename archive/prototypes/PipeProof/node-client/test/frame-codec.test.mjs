import test from "node:test";
import assert from "node:assert/strict";
import { FrameDecoder, FrameSizeError, decodeUtf8, encodeFrame } from "../frame-codec.mjs";

const payload = Buffer.from('{"kind":"test"}', "utf8");

test("fragmented frame is reconstructed", () => {
  const frame = encodeFrame(payload);
  const decoder = new FrameDecoder();
  const output = [];
  for (const byte of frame) output.push(...decoder.push(Buffer.from([byte])));
  decoder.finish();
  assert.equal(output.length, 1);
  assert.deepEqual(output[0], payload);
});

test("coalesced frames are separated", () => {
  const decoder = new FrameDecoder();
  const output = decoder.push(Buffer.concat([encodeFrame(payload), encodeFrame(Buffer.from("{}"))]));
  decoder.finish();
  assert.deepEqual(output.map(value => value.toString("utf8")), ['{"kind":"test"}', "{}"]);
});

test("oversized frame is rejected before allocation", () => {
  const header = Buffer.alloc(4);
  header.writeUInt32BE(101, 0);
  const decoder = new FrameDecoder(100);
  assert.throws(() => decoder.push(header), FrameSizeError);
});

test("invalid UTF-8 is rejected", () => {
  assert.throws(() => decodeUtf8(Buffer.from([0xc3, 0x28])), /not valid UTF-8/);
});

test("truncated frame is rejected at end of stream", () => {
  const decoder = new FrameDecoder();
  decoder.push(encodeFrame(payload).subarray(0, 6));
  assert.throws(() => decoder.finish(), /truncated frame/);
});

test("zero-length frame is rejected before allocation", () => {
  const header = Buffer.alloc(4);
  const decoder = new FrameDecoder();
  assert.throws(() => decoder.push(header), FrameSizeError);
});
