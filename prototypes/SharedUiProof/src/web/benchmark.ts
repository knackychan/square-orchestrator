import type { ProofBenchmarkManifest, ProofFixtureState, ProofHostKind, ProofLayoutPreset, ProofTheme } from "../shared/protocol.js";
import { SharedUiProofWorkspace } from "./workspace-app.js";

interface MemorySample {
  readonly jsHeapUsedBytes: number | null;
  readonly jsHeapTotalBytes: number | null;
}

interface MatrixResult {
  readonly terminalCount: number;
  readonly theme: ProofTheme;
  readonly scale: number;
  readonly durationMs: number;
  readonly bytesRendered: number;
  readonly batchesWritten: number;
  readonly memoryBefore: MemorySample;
  readonly memoryAfter: MemorySample;
  readonly accessibilityFailures: readonly string[];
  readonly sequenceCorrect: boolean;
  readonly passed: boolean;
}

export interface SharedUiProofResult {
  readonly schemaVersion: "1.0";
  readonly host: ProofHostKind;
  readonly runId: string;
  readonly startedUtc: string;
  readonly completedUtc: string;
  readonly fixtureId: string;
  readonly expectedFixtureSha256: string;
  readonly actualFixtureSha256: string;
  readonly fixtureParityPassed: boolean;
  readonly matrix: readonly MatrixResult[];
  readonly hiddenPaneThrottle: {
    readonly passed: boolean;
    readonly hiddenBeforeReveal: readonly string[];
    readonly renderedAfterReveal: readonly string[];
  };
  readonly layoutStatePreservation: { readonly passed: boolean; readonly visited: readonly ProofLayoutPreset[] };
  readonly keyboardFocus: { readonly passed: boolean; readonly from: string; readonly to: string };
  readonly controllerLeaseIndicator: { readonly passed: boolean; readonly control: string; readonly elsewhere: string; readonly view: string };
  readonly accessibility: { readonly passed: boolean; readonly failures: readonly string[] };
  readonly overallPassed: boolean;
}

export async function runSharedUiBenchmark(
  workspace: SharedUiProofWorkspace,
  host: ProofHostKind,
  fixture: ProofFixtureState,
  manifest: ProofBenchmarkManifest,
  expectedFixtureSha256: string,
  actualFixtureSha256: string
): Promise<SharedUiProofResult> {
  const startedUtc = new Date().toISOString();
  const matrix: MatrixResult[] = [];
  for (const terminalCount of manifest.terminalCounts) {
    for (const theme of manifest.themes) {
      for (const scale of manifest.scales) {
        workspace.setTerminalCount(terminalCount);
        workspace.setLayout("Operations", false);
        workspace.setTheme(theme);
        workspace.setScale(scale);
        await settleFrames(2);
        const memoryBefore = memorySample();
        const started = performance.now();
        workspace.renderSyntheticOutput(manifest.bytesPerTerminal, manifest.frameBytes);
        await workspace.waitForRendered(manifest.maximumDurationMs);
        const durationMs = performance.now() - started;
        await settleFrames(2);
        const snapshots = workspace.snapshots();
        const accessibilityFailures = workspace.auditAccessibility();
        const bytesRendered = snapshots.reduce((total, snapshot) => total + snapshot.bytesWritten, 0);
        const batchesWritten = snapshots.reduce((total, snapshot) => total + snapshot.batchesWritten, 0);
        const sequenceCorrect = snapshots.every((snapshot) => snapshot.lastSequence === snapshot.renderedThroughSequence
          && snapshot.pendingFrames === 0 && snapshot.pendingBytes === 0 && !snapshot.writing);
        matrix.push(Object.freeze({
          terminalCount,
          theme,
          scale,
          durationMs,
          bytesRendered,
          batchesWritten,
          memoryBefore,
          memoryAfter: memorySample(),
          accessibilityFailures,
          sequenceCorrect,
          passed: durationMs <= manifest.maximumDurationMs
            && sequenceCorrect
            && bytesRendered === terminalCount * manifest.bytesPerTerminal
            && accessibilityFailures.length === 0
        }));
      }
    }
  }

  workspace.setTerminalCount(8);
  workspace.setLayout("Operations", false);
  workspace.setTheme("dark");
  workspace.setScale(1);
  const ids = workspace.terminalIds();
  const visibleIds = new Set(ids.slice(0, 4));
  const hiddenIds = ids.slice(4);
  for (const id of hiddenIds) workspace.setPaneVisible(id, false);
  workspace.renderSyntheticOutput(Math.min(manifest.bytesPerTerminal, 65_536), manifest.frameBytes);
  await workspace.waitForRendered(Math.min(manifest.maximumDurationMs, Math.max(100, manifest.hiddenThrottleMs / 2)), visibleIds);
  const hiddenBeforeReveal = workspace.snapshots()
    .filter((snapshot) => !snapshot.visible
      && snapshot.lastSequence > 0
      && snapshot.renderedThroughSequence < snapshot.lastSequence
      && snapshot.pendingFrames > 0)
    .map((snapshot) => snapshot.terminalId);
  for (const id of hiddenIds) workspace.setPaneVisible(id, true);
  await workspace.waitForRendered(manifest.maximumDurationMs);
  const renderedAfterReveal = workspace.snapshots()
    .filter((snapshot) => snapshot.lastSequence === snapshot.renderedThroughSequence && snapshot.pendingFrames === 0)
    .map((snapshot) => snapshot.terminalId);
  const hiddenPaneThrottle = Object.freeze({
    passed: hiddenBeforeReveal.length === hiddenIds.length && renderedAfterReveal.length === ids.length,
    hiddenBeforeReveal: Object.freeze(hiddenBeforeReveal),
    renderedAfterReveal: Object.freeze(renderedAfterReveal)
  });

  const selectedBefore = workspace.selectedTerminalId();
  const sequenceBefore = workspace.snapshots().map((snapshot) => [snapshot.terminalId, snapshot.lastSequence] as const);
  const visited: ProofLayoutPreset[] = ["Operations", "Focus Agent", "Plan", "Review", "Resources"];
  for (const preset of visited) {
    workspace.setLayout(preset, false);
    await settleFrames(1);
  }
  workspace.setLayout("Operations", false);
  const sequenceAfter = workspace.snapshots().map((snapshot) => [snapshot.terminalId, snapshot.lastSequence] as const);
  const layoutStatePreservation = Object.freeze({
    passed: workspace.selectedTerminalId() === selectedBefore && JSON.stringify(sequenceBefore) === JSON.stringify(sequenceAfter),
    visited: Object.freeze(visited)
  });

  workspace.setTerminalCount(2);
  const focusIds = workspace.terminalIds();
  const from = focusIds[0] ?? "";
  workspace.focusTerminal(from);
  const to = workspace.focusNext();
  const keyboardFocus = Object.freeze({ passed: to !== from && workspace.terminalContainsFocus(to), from, to });

  workspace.setTerminalCount(3);
  const controllerIds = workspace.terminalIds();
  const controlId = controllerIds[0] ?? "";
  const elsewhereId = controllerIds[1] ?? "";
  const viewId = controllerIds[2] ?? "";
  workspace.setController(controlId, "control");
  workspace.setController(elsewhereId, "controlled_elsewhere");
  workspace.setController(viewId, "view");
  const controllerLeaseIndicator = Object.freeze({
    passed: workspace.controllerText(controlId) === "CONTROL"
      && workspace.controllerText(elsewhereId) === "CONTROLLED ELSEWHERE"
      && workspace.controllerText(viewId) === "VIEW",
    control: workspace.controllerText(controlId),
    elsewhere: workspace.controllerText(elsewhereId),
    view: workspace.controllerText(viewId)
  });

  workspace.setTerminalCount(8);
  const accessibilityFailures = workspace.auditAccessibility();
  const accessibility = Object.freeze({ passed: accessibilityFailures.length === 0, failures: accessibilityFailures });
  const fixtureParityPassed = actualFixtureSha256 === expectedFixtureSha256;
  const overallPassed = fixtureParityPassed
    && matrix.every((result) => result.passed)
    && hiddenPaneThrottle.passed
    && layoutStatePreservation.passed
    && keyboardFocus.passed
    && controllerLeaseIndicator.passed
    && accessibility.passed;

  return Object.freeze({
    schemaVersion: "1.0",
    host,
    runId: manifest.runId,
    startedUtc,
    completedUtc: new Date().toISOString(),
    fixtureId: fixture.fixtureId,
    expectedFixtureSha256,
    actualFixtureSha256,
    fixtureParityPassed,
    matrix: Object.freeze(matrix),
    hiddenPaneThrottle,
    layoutStatePreservation,
    keyboardFocus,
    controllerLeaseIndicator,
    accessibility,
    overallPassed
  });
}

function memorySample(): MemorySample {
  const memory = (performance as Performance & {
    readonly memory?: { readonly usedJSHeapSize: number; readonly totalJSHeapSize: number };
  }).memory;
  return Object.freeze({
    jsHeapUsedBytes: memory?.usedJSHeapSize ?? null,
    jsHeapTotalBytes: memory?.totalJSHeapSize ?? null
  });
}

async function settleFrames(count: number): Promise<void> {
  for (let index = 0; index < count; index++) {
    await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
  }
}
