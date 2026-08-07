export interface TerminalFrame {
  readonly sequence: number;
  readonly bytes: Uint8Array;
}

export interface ScheduledWork { cancel(): void; }
export interface RenderTimer { schedule(callback: () => void, delayMs: number): ScheduledWork; }
export interface RenderSink { write(bytes: Uint8Array, completed: () => void): void; }

export interface RenderQueueSnapshot {
  readonly visible: boolean;
  readonly lastSequence: number;
  readonly renderedThroughSequence: number;
  readonly pendingFrames: number;
  readonly pendingBytes: number;
  readonly batchesWritten: number;
  readonly bytesWritten: number;
  readonly writing: boolean;
}

export type EnqueueResult =
  | { readonly kind: "accepted"; readonly sequence: number }
  | { readonly kind: "duplicate"; readonly sequence: number }
  | { readonly kind: "gap"; readonly expected: number; readonly received: number }
  | { readonly kind: "overflow"; readonly pendingBytes: number; readonly limitBytes: number };

/**
 * Bounded, sequence-preserving presentation queue. Hidden panes render less often, but a sequence is
 * never silently dropped. Gap and overflow outcomes force the caller to request replay or fail the proof.
 */
export class TerminalRenderQueue {
  readonly #sink: RenderSink;
  readonly #timer: RenderTimer;
  readonly #maximumPendingBytes: number;
  readonly #visibleDelayMs: number;
  readonly #hiddenDelayMs: number;
  #visible: boolean;
  #pending: TerminalFrame[] = [];
  #pendingBytes = 0;
  #lastSequence = 0;
  #renderedThroughSequence = 0;
  #batchesWritten = 0;
  #bytesWritten = 0;
  #scheduled: ScheduledWork | null = null;
  #writing = false;
  #disposed = false;

  constructor(
    sink: RenderSink,
    timer: RenderTimer,
    options: {
      readonly maximumPendingBytes: number;
      readonly hiddenDelayMs: number;
      readonly visibleDelayMs?: number;
      readonly initiallyVisible?: boolean;
    }
  ) {
    requirePositiveInteger(options.maximumPendingBytes, "maximumPendingBytes");
    requireNonNegativeInteger(options.hiddenDelayMs, "hiddenDelayMs");
    requireNonNegativeInteger(options.visibleDelayMs ?? 0, "visibleDelayMs");
    if (options.hiddenDelayMs < (options.visibleDelayMs ?? 0)) {
      throw new RangeError("hiddenDelayMs must not be lower than visibleDelayMs");
    }
    this.#sink = sink;
    this.#timer = timer;
    this.#maximumPendingBytes = options.maximumPendingBytes;
    this.#hiddenDelayMs = options.hiddenDelayMs;
    this.#visibleDelayMs = options.visibleDelayMs ?? 0;
    this.#visible = options.initiallyVisible ?? true;
  }

  enqueue(frame: TerminalFrame): EnqueueResult {
    this.#throwIfDisposed();
    requirePositiveInteger(frame.sequence, "sequence");
    if (!(frame.bytes instanceof Uint8Array)) throw new TypeError("bytes must be Uint8Array");
    if (frame.sequence <= this.#lastSequence) return Object.freeze({ kind: "duplicate", sequence: frame.sequence });
    const expected = this.#lastSequence + 1;
    if (frame.sequence !== expected) return Object.freeze({ kind: "gap", expected, received: frame.sequence });
    if (this.#pendingBytes + frame.bytes.byteLength > this.#maximumPendingBytes) {
      return Object.freeze({ kind: "overflow", pendingBytes: this.#pendingBytes, limitBytes: this.#maximumPendingBytes });
    }
    const copy = Object.freeze({ sequence: frame.sequence, bytes: new Uint8Array(frame.bytes) });
    this.#pending.push(copy);
    this.#pendingBytes += copy.bytes.byteLength;
    this.#lastSequence = copy.sequence;
    this.#ensureScheduled();
    return Object.freeze({ kind: "accepted", sequence: copy.sequence });
  }

  setVisible(visible: boolean): void {
    this.#throwIfDisposed();
    if (visible === this.#visible) return;
    this.#visible = visible;
    this.#scheduled?.cancel();
    this.#scheduled = null;
    this.#ensureScheduled();
  }

  flushNow(): void {
    this.#throwIfDisposed();
    this.#scheduled?.cancel();
    this.#scheduled = null;
    this.#flush();
  }

  snapshot(): RenderQueueSnapshot {
    return Object.freeze({
      visible: this.#visible,
      lastSequence: this.#lastSequence,
      renderedThroughSequence: this.#renderedThroughSequence,
      pendingFrames: this.#pending.length,
      pendingBytes: this.#pendingBytes,
      batchesWritten: this.#batchesWritten,
      bytesWritten: this.#bytesWritten,
      writing: this.#writing
    });
  }

  dispose(): void {
    if (this.#disposed) return;
    this.#disposed = true;
    this.#scheduled?.cancel();
    this.#scheduled = null;
    this.#pending = [];
    this.#pendingBytes = 0;
  }

  #ensureScheduled(): void {
    if (this.#disposed || this.#writing || this.#scheduled !== null || this.#pending.length === 0) return;
    const delay = this.#visible ? this.#visibleDelayMs : this.#hiddenDelayMs;
    this.#scheduled = this.#timer.schedule(() => {
      this.#scheduled = null;
      this.#flush();
    }, delay);
  }

  #flush(): void {
    if (this.#disposed || this.#writing || this.#pending.length === 0) return;
    const frames = this.#pending;
    const bytes = concatenate(frames, this.#pendingBytes);
    const through = frames.at(-1)?.sequence ?? this.#renderedThroughSequence;
    this.#pending = [];
    this.#pendingBytes = 0;
    this.#writing = true;
    let completed = false;
    const done = (): void => {
      if (completed) return;
      completed = true;
      this.#writing = false;
      this.#renderedThroughSequence = Math.max(this.#renderedThroughSequence, through);
      this.#batchesWritten++;
      this.#bytesWritten += bytes.byteLength;
      this.#ensureScheduled();
    };
    try {
      this.#sink.write(bytes, done);
    } catch (error) {
      this.#writing = false;
      this.#pending = [...frames, ...this.#pending];
      this.#pendingBytes += bytes.byteLength;
      throw error;
    }
  }

  #throwIfDisposed(): void {
    if (this.#disposed) throw new Error("TerminalRenderQueue is disposed");
  }
}

function concatenate(frames: readonly TerminalFrame[], length: number): Uint8Array {
  const result = new Uint8Array(length);
  let offset = 0;
  for (const frame of frames) {
    result.set(frame.bytes, offset);
    offset += frame.bytes.byteLength;
  }
  return result;
}

function requirePositiveInteger(value: number, name: string): void {
  if (!Number.isSafeInteger(value) || value <= 0) throw new RangeError(`${name} must be a positive integer`);
}

function requireNonNegativeInteger(value: number, name: string): void {
  if (!Number.isSafeInteger(value) || value < 0) throw new RangeError(`${name} must be a non-negative integer`);
}
