export interface TerminalFrame {
  readonly sequence: number;
  readonly bytes: Uint8Array;
  /** Last sequence explicitly unavailable before this frame. */
  readonly truncatedBefore?: number;
}

export interface TerminalStreamState {
  readonly frames: readonly TerminalFrame[];
  readonly lastSequence: number;
  readonly gap: { readonly expected: number; readonly received: number } | null;
  /** Last sequence discarded or declared unavailable. */
  readonly truncatedBefore: number | null;
  readonly retainedBytes: number;
}

export type ControllerMode = "view" | "control" | "controlled_elsewhere";

export function createTerminalStreamState(): TerminalStreamState {
  return Object.freeze({ frames: [], lastSequence: 0, gap: null, truncatedBefore: null, retainedBytes: 0 });
}

export function applyTerminalFrame(
  state: TerminalStreamState,
  frame: TerminalFrame,
  maximumRetainedBytes = 1_048_576
): TerminalStreamState {
  requirePositiveSafeInteger(frame.sequence, "sequence");
  requirePositiveSafeInteger(maximumRetainedBytes, "maximumRetainedBytes");
  if (!(frame.bytes instanceof Uint8Array)) throw new TypeError("bytes must be Uint8Array");
  if (frame.truncatedBefore !== undefined) {
    requireNonNegativeSafeInteger(frame.truncatedBefore, "truncatedBefore");
    if (frame.truncatedBefore >= frame.sequence) throw new RangeError("truncatedBefore must precede sequence");
  }
  if (frame.sequence <= state.lastSequence) return state;

  const expected = state.lastSequence + 1;
  const missingRangeIsExplicit = frame.sequence !== expected && frame.truncatedBefore === frame.sequence - 1;
  if (frame.sequence !== expected && !missingRangeIsExplicit) {
    return Object.freeze({ ...state, gap: { expected, received: frame.sequence } });
  }

  const copiedFrame = Object.freeze({ ...frame, bytes: new Uint8Array(frame.bytes) });
  const appended = [...state.frames, copiedFrame];
  let retainedBytes = state.retainedBytes + frame.bytes.byteLength;
  let remove = 0;
  let truncatedBefore = maximumNullable(state.truncatedBefore, frame.truncatedBefore ?? null);

  while (retainedBytes > maximumRetainedBytes && remove < appended.length) {
    retainedBytes -= appended[remove]?.bytes.byteLength ?? 0;
    truncatedBefore = maximumNullable(truncatedBefore, appended[remove]?.sequence ?? null);
    remove++;
  }

  return Object.freeze({
    frames: Object.freeze(appended.slice(remove)),
    lastSequence: frame.sequence,
    gap: null,
    truncatedBefore,
    retainedBytes
  });
}

function maximumNullable(left: number | null, right: number | null): number | null {
  if (left === null) return right;
  if (right === null) return left;
  return Math.max(left, right);
}

function requirePositiveSafeInteger(value: number, name: string): void {
  if (!Number.isSafeInteger(value) || value <= 0) throw new RangeError(`${name} must be a positive integer`);
}

function requireNonNegativeSafeInteger(value: number, name: string): void {
  if (!Number.isSafeInteger(value) || value < 0) throw new RangeError(`${name} must be a non-negative integer`);
}
