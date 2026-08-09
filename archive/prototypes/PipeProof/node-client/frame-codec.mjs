import { TextDecoder } from "node:util";

export const DEFAULT_MAXIMUM_PAYLOAD_BYTES = 1_048_576;
const strictUtf8 = new TextDecoder("utf-8", { fatal: true, ignoreBOM: true });

export class FrameSizeError extends Error {
  constructor(declaredLength, maximumLength) {
    super(`Frame payload length ${declaredLength} is outside the allowed range 1..${maximumLength}.`);
    this.name = "FrameSizeError";
    this.declaredLength = declaredLength;
    this.maximumLength = maximumLength;
  }
}

export function encodeFrame(payload, maximumPayloadBytes = DEFAULT_MAXIMUM_PAYLOAD_BYTES) {
  const bytes = Buffer.isBuffer(payload) ? payload : Buffer.from(payload);
  if (bytes.length <= 0 || bytes.length > maximumPayloadBytes) {
    throw new FrameSizeError(bytes.length, maximumPayloadBytes);
  }
  const frame = Buffer.allocUnsafe(4 + bytes.length);
  frame.writeUInt32BE(bytes.length, 0);
  bytes.copy(frame, 4);
  return frame;
}

export function decodeUtf8(payload) {
  try {
    return strictUtf8.decode(payload);
  } catch (error) {
    throw new Error("Protocol payload is not valid UTF-8.", { cause: error });
  }
}

export class FrameDecoder {
  #maximumPayloadBytes;
  #buffer = Buffer.alloc(0);
  #expectedLength = null;

  constructor(maximumPayloadBytes = DEFAULT_MAXIMUM_PAYLOAD_BYTES) {
    if (!Number.isSafeInteger(maximumPayloadBytes) || maximumPayloadBytes <= 0) {
      throw new RangeError("maximumPayloadBytes must be a positive safe integer");
    }
    this.#maximumPayloadBytes = maximumPayloadBytes;
  }

  push(chunk) {
    const bytes = Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk);
    if (bytes.length > 0) {
      this.#buffer = this.#buffer.length === 0 ? bytes : Buffer.concat([this.#buffer, bytes]);
    }
    const payloads = [];
    while (true) {
      if (this.#expectedLength === null) {
        if (this.#buffer.length < 4) break;
        const length = this.#buffer.readUInt32BE(0);
        this.#buffer = this.#buffer.subarray(4);
        if (length <= 0 || length > this.#maximumPayloadBytes) {
          throw new FrameSizeError(length, this.#maximumPayloadBytes);
        }
        this.#expectedLength = length;
      }
      if (this.#buffer.length < this.#expectedLength) break;
      payloads.push(Buffer.from(this.#buffer.subarray(0, this.#expectedLength)));
      this.#buffer = this.#buffer.subarray(this.#expectedLength);
      this.#expectedLength = null;
    }
    return payloads;
  }

  finish() {
    if (this.#expectedLength !== null || this.#buffer.length !== 0) {
      throw new Error("The framed stream ended with a truncated frame.");
    }
  }
}
