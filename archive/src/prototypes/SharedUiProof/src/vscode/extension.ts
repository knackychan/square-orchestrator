import { createHash, randomBytes } from "node:crypto";
import { dirname } from "node:path";
import * as vscode from "vscode";
import {
  SHARED_UI_PROOF_PROTOCOL,
  parseBenchmark,
  parseFixture,
  parseUiToHostMessage,
  type ProofBenchmarkManifest,
  type ProofFixtureState,
  type UiToHostMessage
} from "../shared/protocol.js";

const commandId = "square.sharedUiProof.open";
let activePanel: vscode.WebviewPanel | null = null;

export function activate(context: vscode.ExtensionContext): void {
  context.subscriptions.push(vscode.commands.registerCommand(commandId, () => openProof(context, false)));
  if (process.env.SQUARE_SHARED_UI_PROOF_AUTORUN === "1") {
    queueMicrotask(() => {
      void openProof(context, true).catch(async (error: unknown) => {
        await vscode.window.showErrorMessage(`Square Shared UI Proof failed: ${errorMessage(error)}`);
      });
    });
  }
}

export function deactivate(): void {
  activePanel?.dispose();
  activePanel = null;
}

async function openProof(context: vscode.ExtensionContext, autorun: boolean): Promise<void> {
  if (activePanel !== null) {
    activePanel.reveal(vscode.ViewColumn.One, false);
    return;
  }

  const fixtureBytes = await vscode.workspace.fs.readFile(vscode.Uri.joinPath(context.extensionUri, "fixtures", "canonical-state.json"));
  const benchmarkBytes = await vscode.workspace.fs.readFile(vscode.Uri.joinPath(context.extensionUri, "fixtures", "benchmark-manifest.json"));
  const fixture = parseFixture(JSON.parse(new TextDecoder().decode(fixtureBytes)));
  const benchmark = parseBenchmark(JSON.parse(new TextDecoder().decode(benchmarkBytes)));
  const expectedFixtureSha256 = new TextDecoder().decode(
    await vscode.workspace.fs.readFile(vscode.Uri.joinPath(context.extensionUri, "fixtures", "canonical-state.sha256"))
  ).trim();
  const webRoot = vscode.Uri.joinPath(context.extensionUri, "dist");
  const panel = vscode.window.createWebviewPanel(
    "square.sharedUiProof",
    "Square Shared UI Proof",
    vscode.ViewColumn.One,
    {
      enableScripts: true,
      retainContextWhenHidden: true,
      localResourceRoots: [webRoot]
    }
  );
  activePanel = panel;

  const failures: string[] = [];
  let terminalResizeMessages = 0;
  let terminalInputMessages = 0;
  let layoutMessages = 0;
  let controllerRequests = 0;
  let completed = false;
  let result: unknown = null;
  const startedAtUtc = new Date().toISOString();
  const acceptanceRun = process.env.SQUARE_SHARED_UI_PROOF_ACCEPTANCE === "1";
  const watchdog = setTimeout(() => {
    failures.push("Shared UI proof exceeded the ten-minute host deadline");
    void finish();
  }, 10 * 60 * 1_000);

  const messageSubscription = panel.webview.onDidReceiveMessage(async (value: unknown) => {
    let message: UiToHostMessage;
    try {
      message = parseUiToHostMessage(value);
    } catch (error) {
      failures.push(`Rejected webview message: ${errorMessage(error)}`);
      await finish();
      return;
    }

    switch (message.type) {
      case "proof.ready":
        if (message.host !== "vscode") {
          failures.push(`Webview reported unexpected host '${message.host}'`);
          await finish();
          return;
        }
        try {
          await postInitialization(panel, fixture, benchmark, expectedFixtureSha256);
        } catch (error) {
          failures.push(`Host initialization message failed: ${errorMessage(error)}`);
          await finish();
        }
        break;
      case "proof.result":
        if (message.runId !== benchmark.runId || message.fixtureSha256 !== expectedFixtureSha256) {
          failures.push("Result identity did not match the canonical run or fixture");
        }
        result = message.result;
        if (!proofResultPassed(message.result)) failures.push("Shared UI result reported failure");
        await finish();
        break;
      case "proof.error":
        failures.push(`${message.code}: ${message.message}`);
        await finish();
        break;
      case "terminal.resize":
        terminalResizeMessages++;
        break;
      case "terminal.input":
        terminalInputMessages++;
        break;
      case "proof.layoutChanged":
        layoutMessages++;
        break;
      case "proof.controllerRequested":
        controllerRequests++;
        break;
    }
  });

  const panelSubscription = panel.onDidDispose(() => {
    activePanel = null;
    messageSubscription.dispose();
    if (!completed) {
      failures.push("VS Code webview closed before the proof completed");
      void finish();
    }
  });
  context.subscriptions.push(messageSubscription, panelSubscription, panel);
  panel.webview.html = await createWebviewHtml(panel.webview, webRoot);

  async function finish(): Promise<void> {
    if (completed) return;
    completed = true;
    clearTimeout(watchdog);
    const passed = failures.length === 0 && proofResultPassed(result);
    const requestedEvidence = process.env.SQUARE_SHARED_UI_PROOF_EVIDENCE;
    const evidenceUri = requestedEvidence === undefined || requestedEvidence.trim().length === 0
      ? vscode.Uri.joinPath(context.globalStorageUri, "sp00-t04-vscode.json")
      : vscode.Uri.file(requestedEvidence);
    await writeJsonAtomic(evidenceUri, {
      schemaVersion: "1.0",
      taskId: "SP00-T04",
      hostKind: "vscode",
      status: passed ? "PASS" : "FAIL",
      acceptanceEligible: passed && acceptanceRun,
      startedAtUtc,
      completedAtUtc: new Date().toISOString(),
      fixtureSha256: expectedFixtureSha256,
      benchmarkSha256: sha256(benchmarkBytes),
      environment: {
        vscodeVersion: vscode.version,
        nodeVersion: process.version,
        platform: process.platform,
        architecture: process.arch,
        chromiumVersion: process.versions.chrome ?? null,
        memory: process.memoryUsage()
      },
      bridgeEvents: {
        terminalResizeMessages,
        terminalInputMessages,
        layoutMessages,
        controllerRequests
      },
      result,
      failures,
      passed
    });
    if (!autorun) await vscode.window.showInformationMessage(`SP00-T04 VS Code evidence written to ${evidenceUri.fsPath}`);
    if (autorun) {
      panel.dispose();
      setTimeout(() => { void vscode.commands.executeCommand("workbench.action.closeWindow"); }, 250);
    }
  }
}

async function postInitialization(
  panel: vscode.WebviewPanel,
  fixture: ProofFixtureState,
  benchmark: ProofBenchmarkManifest,
  expectedFixtureSha256: string
): Promise<void> {
  const initialized = await panel.webview.postMessage({
    version: SHARED_UI_PROOF_PROTOCOL,
    type: "proof.initialize",
    host: "vscode",
    fixture,
    benchmark,
    expectedFixtureSha256
  });
  if (!initialized) throw new Error("VS Code rejected proof.initialize");
  const started = await panel.webview.postMessage({
    version: SHARED_UI_PROOF_PROTOCOL,
    type: "proof.runBenchmark",
    runId: benchmark.runId
  });
  if (!started) throw new Error("VS Code rejected proof.runBenchmark");
}

async function createWebviewHtml(webview: vscode.Webview, webRoot: vscode.Uri): Promise<string> {
  const templateBytes = await vscode.workspace.fs.readFile(vscode.Uri.joinPath(webRoot, "index.template.html"));
  const nonce = randomBytes(18).toString("base64url");
  const asset = (...segments: string[]): string => webview.asWebviewUri(vscode.Uri.joinPath(webRoot, ...segments)).toString(true);
  const csp = [
    "default-src 'none'",
    `img-src ${webview.cspSource} data:`,
    `style-src ${webview.cspSource}`,
    "style-src-attr 'unsafe-inline'",
    `font-src ${webview.cspSource}`,
    `script-src ${webview.cspSource} 'nonce-${nonce}'`,
    "connect-src 'none'",
    "object-src 'none'",
    "base-uri 'none'",
    "form-action 'none'",
    "frame-ancestors 'none'"
  ].join("; ");
  return replaceTemplate(new TextDecoder().decode(templateBytes), {
    CSP: csp,
    NONCE: nonce,
    XTERM_CSS_URI: asset("vendor", "xterm.css"),
    STYLES_URI: asset("styles.css"),
    XTERM_MODULE_URI: asset("vendor", "xterm.mjs"),
    FIT_MODULE_URI: asset("vendor", "addon-fit.mjs"),
    APP_MODULE_URI: asset("src", "web", "main.js")
  });
}

function replaceTemplate(template: string, values: Readonly<Record<string, string>>): string {
  let result = template;
  for (const [key, value] of Object.entries(values)) result = result.replaceAll(`{{${key}}}`, escapeTemplateValue(value));
  if (/\{\{[A-Z0-9_]+\}\}/.test(result)) throw new Error("Shared UI HTML template contains an unresolved placeholder");
  return result;
}

function escapeTemplateValue(value: string): string {
  // Values are generated locally. Encode only characters that could terminate the quoted attribute;
  // do not HTML-encode '&' because VS Code webview resource URIs may contain query separators.
  return value.replaceAll('"', "%22").replaceAll("<", "%3C").replaceAll(">", "%3E");
}

async function writeJsonAtomic(uri: vscode.Uri, value: unknown): Promise<void> {
  await vscode.workspace.fs.createDirectory(vscode.Uri.file(dirname(uri.fsPath)));
  const temporary = vscode.Uri.file(`${uri.fsPath}.${randomBytes(8).toString("hex")}.tmp`);
  const bytes = new TextEncoder().encode(`${JSON.stringify(value, null, 2)}\n`);
  await vscode.workspace.fs.writeFile(temporary, bytes);
  await vscode.workspace.fs.rename(temporary, uri, { overwrite: true });
}

function proofResultPassed(value: unknown): boolean {
  return typeof value === "object" && value !== null && !Array.isArray(value)
    && (value as Readonly<Record<string, unknown>>).overallPassed === true;
}

function sha256(bytes: Uint8Array): string {
  return createHash("sha256").update(bytes).digest("hex");
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
