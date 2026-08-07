import type { ProofFixtureState, ProofTerminalFixture } from "./protocol.js";

export interface SyntheticFrame {
  readonly sequence: number;
  readonly bytes: Uint8Array;
}

export function createSyntheticFrames(
  terminal: ProofTerminalFixture,
  bytesTarget: number,
  frameBytes: number,
  startingSequence = 1
): readonly SyntheticFrame[] {
  requirePositiveInteger(bytesTarget, "bytesTarget");
  requirePositiveInteger(frameBytes, "frameBytes");
  requirePositiveInteger(startingSequence, "startingSequence");
  const encoder = new TextEncoder();
  const frames: SyntheticFrame[] = [];
  let produced = 0;
  let sequence = startingSequence;
  while (produced < bytesTarget) {
    const line = `\u001b[36m[${terminal.taskId}]\u001b[0m ${terminal.role} · ${terminal.route} · frame ${sequence} — 測試 ✓\r\n`
      + `${terminal.id}:${"0123456789abcdef".repeat(32)}\r\n`;
    const source = encoder.encode(line);
    const desired = Math.min(frameBytes, bytesTarget - produced);
    const bytes = new Uint8Array(desired);
    for (let offset = 0; offset < desired; offset++) bytes[offset] = source[offset % source.byteLength] ?? 0x20;
    frames.push(Object.freeze({ sequence, bytes }));
    produced += desired;
    sequence++;
  }
  return Object.freeze(frames);
}

export function validateFixtureAccessibility(fixture: ProofFixtureState): readonly string[] {
  const failures: string[] = [];
  for (const terminal of fixture.terminals) {
    for (const required of [terminal.taskId, terminal.role, terminal.route, terminal.state]) {
      if (!terminal.ariaLabel.toLocaleLowerCase().includes(required.toLocaleLowerCase())) {
        failures.push(`${terminal.id} ariaLabel omits '${required}'`);
      }
    }
    const controller = terminal.controllerMode.replaceAll("_", " ");
    if (!terminal.ariaLabel.toLocaleLowerCase().includes(controller)) {
      failures.push(`${terminal.id} ariaLabel omits controller mode '${controller}'`);
    }
  }
  return Object.freeze(failures);
}

function requirePositiveInteger(value: number, name: string): void {
  if (!Number.isSafeInteger(value) || value <= 0) throw new RangeError(`${name} must be a positive integer`);
}
