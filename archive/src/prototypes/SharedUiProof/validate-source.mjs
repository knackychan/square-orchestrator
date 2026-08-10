import { spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { readFile, readdir } from "node:fs/promises";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const proofRoot = resolve(dirname(fileURLToPath(import.meta.url)));
const repositoryRoot = resolve(proofRoot, "../..");
const failures = [];

const required = [
  "package.json",
  "tsconfig.json",
  "bridge-contract.json",
  "scenario-manifest.json",
  "evidence.schema.json",
  "dispatch.packet.json",
  "source-manifest.sha256",
  "web/index.template.html",
  "web/styles.css",
  "fixtures/canonical-state.json",
  "fixtures/canonical-state.sha256",
  "fixtures/benchmark-manifest.json",
  "src/shared/protocol.ts",
  "src/shared/fixture.ts",
  "src/web/render-queue.ts",
  "src/web/terminal-pane.ts",
  "src/web/workspace-app.ts",
  "src/web/benchmark.ts",
  "src/web/main.ts",
  "src/vscode/extension.ts",
  "webview2-host/Square.SharedUiProof.WebView2/Square.SharedUiProof.WebView2.csproj",
  "webview2-host/Square.SharedUiProof.WebView2/MainWindow.xaml.cs",
  "run-proof.ps1",
  "scripts/compare-evidence.mjs"
];
for (const path of required) await requireFile(path);
const manifestValidation = spawnSync(process.execPath, [join(proofRoot, "scripts", "source-manifest.mjs")], {
  cwd: repositoryRoot,
  encoding: "utf8"
});
if (manifestValidation.status !== 0) {
  failures.push((manifestValidation.stderr || manifestValidation.stdout || "source manifest validation failed").trim());
}

const packageJson = await json("package.json");
const fixture = await json("fixtures/canonical-state.json");
const benchmark = await json("fixtures/benchmark-manifest.json");
const scenarios = await json("scenario-manifest.json");
const bridge = await json("bridge-contract.json");
const rootLock = await text(resolve(repositoryRoot, "pnpm-lock.yaml"));
const template = await text(join(proofRoot, "web", "index.template.html"));
const extension = await text(join(proofRoot, "src", "vscode", "extension.ts"));
const webBridge = await text(join(proofRoot, "src", "web", "bridge.ts"));
const mainWindow = await text(join(proofRoot, "webview2-host", "Square.SharedUiProof.WebView2", "MainWindow.xaml.cs"));
const bridgeValidator = await text(join(proofRoot, "webview2-host", "Square.SharedUiProof.WebView2", "BridgeValidator.cs"));
const csproj = await text(join(proofRoot, "webview2-host", "Square.SharedUiProof.WebView2", "Square.SharedUiProof.WebView2.csproj"));
const htmlRenderer = await text(join(proofRoot, "webview2-host", "Square.SharedUiProof.WebView2", "HtmlTemplateRenderer.cs"));

check(packageJson.dependencies?.["@xterm/xterm"] === "6.0.0", "@xterm/xterm must be pinned to 6.0.0");
check(packageJson.dependencies?.["@xterm/addon-fit"] === "0.11.0", "@xterm/addon-fit must be pinned to 0.11.0");
check(rootLock.includes("@xterm/xterm@6.0.0") && rootLock.includes("@xterm/addon-fit@0.11.0"), "root lockfile omits pinned xterm packages");
check(rootLock.includes("TQwDdQGtwwDt+2cgKDLn0IRaSxYu1tSUjgKarSDkUM0ZNiSRXFpjxEsvc/Zgc5kq5omJ+V0a8/kIM2WD3sMOYg=="), "xterm lock integrity is missing");
check(rootLock.includes("jYcgT6xtVYhnhgxh3QgYDnnNMYTcf8ElbxxFzX0IZo+vabQqSPAjC3c1wJrKB5E19VwQei89QCiZZP86DCPF7g=="), "addon-fit lock integrity is missing");

check(fixture.schemaVersion === "1.0" && fixture.fixtureId === "sp00-t04-canonical-workspace", "canonical fixture identity is invalid");
check(Array.isArray(fixture.terminals) && fixture.terminals.length === 8, "canonical fixture must contain eight terminals");
check(new Set(fixture.terminals?.map((terminal) => terminal.id)).size === 8, "canonical terminal IDs are not unique");
const expectedFixtureHash = (await text(join(proofRoot, "fixtures", "canonical-state.sha256"))).trim();
const actualFixtureHash = createHash("sha256").update(canonicalJson(fixture)).digest("hex");
check(actualFixtureHash === expectedFixtureHash, "canonical fixture hash does not match canonical-state.sha256");

check(equalSet(benchmark.terminalCounts, [1, 4, 8]), "benchmark must cover one, four, and eight terminals");
check(equalSet(benchmark.themes, ["dark", "light", "high-contrast"]), "benchmark must cover dark, light, and high contrast");
check(equalSet(benchmark.scales, [1, 1.5, 2]), "benchmark must cover 100%, 150%, and 200% scale");
check(benchmark.maximumPendingBytes > benchmark.bytesPerTerminal, "pending-byte bound must accommodate one declared burst per terminal");
check(equalSet(scenarios.requiredHosts, ["webview2", "vscode"]), "scenario manifest must require both hosts");
check(Array.isArray(scenarios.acceptanceChecks) && scenarios.acceptanceChecks.includes("hidden_pane_throttle_without_sequence_loss"), "scenario manifest omits hidden-pane sequence check");

check(bridge.protocol === "square.shared-ui-proof/1", "bridge protocol is not canonical");
check(bridge.unknownTypes === "reject" && bridge.unknownFields === "reject", "bridge must reject unknown types and fields");
check(!Object.keys(bridge.hostToUi ?? {}).some((name) => /shell|command/i.test(name)), "host bridge contains an arbitrary command message");
check(!Object.keys(bridge.uiToHost ?? {}).some((name) => /shell|command/i.test(name)), "UI bridge contains an arbitrary command message");

for (const placeholder of ["CSP", "NONCE", "XTERM_CSS_URI", "STYLES_URI", "XTERM_MODULE_URI", "FIT_MODULE_URI", "APP_MODULE_URI"]) {
  check(template.includes(`{{${placeholder}}}`), `HTML template omits ${placeholder}`);
}
check(!/https?:\/\//i.test(template), "HTML template must not contain a remote runtime URL");
check(template.includes('type="importmap"') && template.includes('nonce="{{NONCE}}"'), "import map is not nonce protected");
check(extension.includes("parseUiToHostMessage"), "VS Code host does not use strict UI message parsing");
check(extension.includes("SQUARE_SHARED_UI_PROOF_ACCEPTANCE") && extension.includes("passed && acceptanceRun"), "VS Code evidence is not fail-closed to the verified runner");
check(extension.includes("localResourceRoots: [webRoot]"), "VS Code webview does not restrict local resource roots");
check(extension.includes("connect-src 'none'") && extension.includes("object-src 'none'"), "VS Code CSP is incomplete");
check(extension.includes("style-src-attr 'unsafe-inline'") && !/`style-src\s+[^`\n]*unsafe-inline/.test(extension), "VS Code CSP must allow style attributes without broadly allowing inline stylesheets");
check(!/child_process|exec\(|spawn\(|createTerminal\(/.test(extension), "VS Code host exposes a process or terminal command path");
check(webBridge.includes("parseHostToUiMessage"), "web content does not strictly parse host messages");

for (const marker of [
  "SetVirtualHostNameToFolderMapping",
  "CoreWebView2HostResourceAccessKind.DenyCors",
  "NavigationStarting",
  "NewWindowRequested",
  "DownloadStarting",
  "PermissionRequested",
  "AreHostObjectsAllowed = false",
  "IsWebMessageEnabled = true"
]) check(mainWindow.includes(marker), `WebView2 host is missing security marker '${marker}'`);
check(bridgeValidator.includes("Unknown UI-to-host message type") && bridgeValidator.includes("Invalid fields"), "WebView2 bridge validator is not fail-closed");
check(htmlRenderer.includes("style-src-attr 'unsafe-inline'") && !/style-src \{origin\}[^\n]*unsafe-inline/.test(htmlRenderer), "WebView2 CSP must allow style attributes without broadly allowing inline stylesheets");
check(csproj.includes('Microsoft.Web.WebView2" Version="1.0.4129.50"'), "WebView2 package is not pinned to 1.0.4129.50");
check(mainWindow.includes("passed && _options.AcceptanceRun"), "WebView2 evidence is not fail-closed to the verified runner");
check(mainWindow.includes("internal MainWindow(ProgramOptions options)"), "WebView2 window exposes an inconsistent public constructor");
check(mainWindow.includes('Path.Combine(webRoot, "index.template.html")'), "WebView2 host does not load the shared template");
check(extension.includes('"index.template.html"'), "VS Code host does not load the shared template");

const productionFiles = await filesUnder(["src", "ui", "vscode"]);
for (const file of productionFiles) {
  if (!/\.(?:csproj|cs|ts|json|xaml)$/.test(file)) continue;
  const source = await text(resolve(repositoryRoot, file));
  check(!source.includes("SharedUiProof"), `production source references prototype: ${file}`);
}
const prototypeSolution = await text(resolve(repositoryRoot, "prototypes", "SquareOrchestrator.Prototypes.slnx"));
check(prototypeSolution.includes("SharedUiProof/webview2-host/Square.SharedUiProof.WebView2/Square.SharedUiProof.WebView2.csproj"), "prototype solution omits SharedUiProof WebView2 host");

const sources = (await filesUnder(["prototypes/SharedUiProof/src", "prototypes/SharedUiProof/webview2-host"]))
  .filter((path) => /\.(?:ts|cs|xaml|csproj)$/.test(path));
if (failures.length > 0) {
  console.error(`SharedUiProof source validation failed (${failures.length}):`);
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}
console.log(`SharedUiProof source contract passed: ${sources.length} source/project files, 8 terminals, 27 render matrix cells, 2 isolated hosts.`);

function check(condition, message) { if (!condition) failures.push(message); }
async function requireFile(path) {
  try { await readFile(join(proofRoot, path)); }
  catch { failures.push(`missing required file: ${path}`); }
}
async function json(path) { return JSON.parse(await text(join(proofRoot, path))); }
async function text(path) { return readFile(path, "utf8"); }
function canonicalJson(value) {
  if (Array.isArray(value)) return `[${value.map(canonicalJson).join(",")}]`;
  if (value && typeof value === "object") return `{${Object.keys(value).sort().map((key) => `${JSON.stringify(key)}:${canonicalJson(value[key])}`).join(",")}}`;
  return JSON.stringify(value);
}
function equalSet(actual, expected) {
  return Array.isArray(actual) && actual.length === expected.length && expected.every((entry) => actual.includes(entry));
}
async function filesUnder(roots) {
  const output = [];
  for (const root of roots) await walk(resolve(repositoryRoot, root), output);
  return output.map((path) => relative(repositoryRoot, path).replaceAll("\\", "/"));
}
async function walk(directory, output) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (["dist", "bin", "obj", "node_modules"].includes(entry.name)) continue;
    const path = join(directory, entry.name);
    if (entry.isDirectory()) await walk(path, output);
    else output.push(path);
  }
}
