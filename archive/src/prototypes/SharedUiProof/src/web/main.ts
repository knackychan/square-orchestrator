import {
  SHARED_UI_PROOF_PROTOCOL,
  sha256Hex,
  type HostToUiMessage,
  type ProofBenchmarkManifest,
  type ProofFixtureState
} from "../shared/protocol.js";
import { runSharedUiBenchmark } from "./benchmark.js";
import { createProofHostBridge } from "./bridge.js";
import { SharedUiProofWorkspace } from "./workspace-app.js";
import { loadXtermFactory } from "./xterm-runtime.js";

const bridge = createProofHostBridge();
const root = document.querySelector<HTMLElement>("#app");
if (root === null) throw new Error("Shared UI proof root #app is missing");

try {
  const workspace = new SharedUiProofWorkspace(root, loadXtermFactory(), (message) => bridge.post(message));
  let fixture: ProofFixtureState | null = null;
  let benchmark: ProofBenchmarkManifest | null = null;
  let expectedFixtureSha256 = "";
  let actualFixtureSha256 = "";
  let running = false;

  bridge.onMessage((message) => { void handleMessage(message); });
  bridge.post({ version: SHARED_UI_PROOF_PROTOCOL, type: "proof.ready", host: bridge.host });

  async function handleMessage(message: HostToUiMessage): Promise<void> {
    try {
      switch (message.type) {
        case "proof.initialize":
          if (message.host !== bridge.host) throw new Error(`Host mismatch: expected ${bridge.host}, received ${message.host}`);
          fixture = message.fixture;
          benchmark = message.benchmark;
          expectedFixtureSha256 = message.expectedFixtureSha256;
          actualFixtureSha256 = await sha256Hex(fixture);
          workspace.initialize(fixture, benchmark);
          return;
        case "proof.setLayout":
          workspace.setLayout(message.preset);
          return;
        case "proof.setTheme":
          workspace.setTheme(message.theme);
          return;
        case "proof.setScale":
          workspace.setScale(message.scale);
          return;
        case "proof.setController":
          workspace.setController(message.terminalId, message.mode);
          return;
        case "proof.runBenchmark":
          if (running) throw new Error("A shared UI benchmark is already running");
          if (fixture === null || benchmark === null) throw new Error("proof.initialize must precede proof.runBenchmark");
          if (message.runId !== benchmark.runId) throw new Error("runId does not match the benchmark manifest");
          running = true;
          try {
            const result = await runSharedUiBenchmark(
              workspace,
              bridge.host,
              fixture,
              benchmark,
              expectedFixtureSha256,
              actualFixtureSha256
            );
            bridge.post({
              version: SHARED_UI_PROOF_PROTOCOL,
              type: "proof.result",
              runId: message.runId,
              fixtureSha256: actualFixtureSha256,
              result
            });
          } finally {
            running = false;
          }
          return;
      }
    } catch (error) {
      bridge.post({
        version: SHARED_UI_PROOF_PROTOCOL,
        type: "proof.error",
        code: "shared_ui_proof_failure",
        message: error instanceof Error ? error.message : String(error)
      });
    }
  }
} catch (error) {
  bridge.post({
    version: SHARED_UI_PROOF_PROTOCOL,
    type: "proof.error",
    code: "shared_ui_bootstrap_failure",
    message: error instanceof Error ? error.message : String(error)
  });
}
