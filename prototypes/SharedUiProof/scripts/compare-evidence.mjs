import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const [webViewPath, vscodePath, outputPath] = process.argv.slice(2);
if (!webViewPath || !vscodePath || !outputPath) {
  console.error("Usage: node compare-evidence.mjs <webview2.json> <vscode.json> <comparison.json>");
  process.exit(2);
}

const webview2 = JSON.parse(await readFile(resolve(webViewPath), "utf8"));
const vscode = JSON.parse(await readFile(resolve(vscodePath), "utf8"));
const failures = [];
for (const [name, evidence, expectedHost] of [["WebView2", webview2, "webview2"], ["VS Code", vscode, "vscode"]]) {
  if (evidence.hostKind !== expectedHost) failures.push(`${name} hostKind is '${String(evidence.hostKind)}'`);
  if (evidence.passed !== true || evidence.acceptanceEligible !== true || evidence.status !== "PASS") {
    failures.push(`${name} evidence is not an acceptance PASS`);
  }
  if (!evidence.result || evidence.result.overallPassed !== true) failures.push(`${name} shared result did not pass`);
}
if (webview2.fixtureSha256 !== vscode.fixtureSha256) failures.push("Fixture hashes differ between hosts");
if (webview2.benchmarkSha256 !== vscode.benchmarkSha256) failures.push("Benchmark hashes differ between hosts");

const webSignature = semanticSignature(webview2.result);
const vscodeSignature = semanticSignature(vscode.result);
if (JSON.stringify(webSignature) !== JSON.stringify(vscodeSignature)) failures.push("Semantic result signatures differ between hosts");

const passed = failures.length === 0;
const comparison = {
  schemaVersion: "1.0",
  taskId: "SP00-T04",
  status: passed ? "PASS" : "FAIL",
  acceptanceEligible: passed,
  comparedAtUtc: new Date().toISOString(),
  fixtureSha256: webview2.fixtureSha256 ?? null,
  benchmarkSha256: webview2.benchmarkSha256 ?? null,
  semanticSignature: webSignature,
  failures,
  passed
};
await writeFile(resolve(outputPath), `${JSON.stringify(comparison, null, 2)}\n`, "utf8");
console.log(`SP00-T04 cross-host comparison: ${comparison.status}`);
if (!passed) process.exit(1);

function semanticSignature(result) {
  if (!result || typeof result !== "object") return null;
  return {
    fixtureId: result.fixtureId,
    fixtureParityPassed: result.fixtureParityPassed,
    matrix: Array.isArray(result.matrix)
      ? result.matrix.map((entry) => ({
          terminalCount: entry.terminalCount,
          theme: entry.theme,
          scale: entry.scale,
          bytesRendered: entry.bytesRendered,
          sequenceCorrect: entry.sequenceCorrect,
          accessibilityFailureCount: Array.isArray(entry.accessibilityFailures) ? entry.accessibilityFailures.length : -1,
          passed: entry.passed
        }))
      : null,
    hiddenPaneThrottle: result.hiddenPaneThrottle?.passed,
    layoutStatePreservation: result.layoutStatePreservation?.passed,
    keyboardFocus: result.keyboardFocus?.passed,
    controllerLeaseIndicator: result.controllerLeaseIndicator?.passed,
    accessibility: result.accessibility?.passed,
    overallPassed: result.overallPassed
  };
}
