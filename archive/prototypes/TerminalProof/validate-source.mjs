import { readFile, readdir, stat } from "node:fs/promises";
import { createHash } from "node:crypto";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = dirname(fileURLToPath(import.meta.url));
const required = [
  "TerminalProof.slnx",
  "README.md",
  "scenarios.json",
  "evidence.schema.json",
  "dispatch.packet.json",
  "run-proof.ps1",
  "Square.TerminalProof.Native/Square.TerminalProof.Native.csproj",
  "Square.TerminalProof.Fixture/Square.TerminalProof.Fixture.csproj",
  "Square.TerminalProof.CrashOwner/Square.TerminalProof.CrashOwner.csproj",
  "Square.TerminalProof.Harness/Square.TerminalProof.Harness.csproj",
  "Square.TerminalProof.Tests/Square.TerminalProof.Tests.csproj"
];

function fail(message) {
  throw new Error(message);
}

async function requireFile(path) {
  const full = join(root, path);
  try {
    if (!(await stat(full)).isFile()) fail(`${path} is not a file`);
  } catch {
    fail(`missing required file ${path}`);
  }
}

for (const path of required) await requireFile(path);

const manifest = JSON.parse(await readFile(join(root, "scenarios.json"), "utf8"));
const expectedScenarios = [
  "unicode",
  "ansi",
  "large_burst",
  "quiet_child",
  "stdin_question",
  "resize",
  "normal_exit",
  "crash",
  "graceful_cancel",
  "forced_termination",
  "nested_children"
];
if (JSON.stringify(manifest.scenarios) !== JSON.stringify(expectedScenarios)) {
  fail(`manifest scenarios must remain the ordered SP00-T02 set: ${expectedScenarios.join(", ")}`);
}
if (manifest.schema_version !== "1.0") fail("manifest schema_version must be 1.0");
if (manifest.repeat_each !== 100) fail("manifest repeat_each must remain 100");
if (manifest.scale_repeat_each !== 1) fail("manifest scale_repeat_each must remain 1");
if (JSON.stringify(manifest.session_counts) !== JSON.stringify([1, 4, 8])) {
  fail("manifest session_counts must be [1,4,8]");
}
for (const metric of ["cpu", "working_set", "output_latency_ms", "bytes_written", "handle_count", "leaked_descendants"]) {
  if (!manifest.required_metrics?.includes(metric)) fail(`manifest is missing required metric ${metric}`);
}

const dispatchPacket = JSON.parse(await readFile(join(root, "dispatch.packet.json"), "utf8"));
if (dispatchPacket.schema_version !== "1.0" || dispatchPacket.task_id !== "SP00-T02") {
  fail("dispatch.packet.json must identify SP00-T02 schema 1.0");
}
const repositoryRoot = resolve(root, "../..");
const authorityManifest = await readFile(join(repositoryRoot, "docs/authority/manifest.sha256"), "utf8");
const authorityHashes = new Map(
  authorityManifest.trim().split(/\r?\n/).map(line => {
    const [hash, name] = line.trim().split(/\s+/, 2);
    return [name, hash];
  })
);
for (const authority of dispatchPacket.authority_documents ?? []) {
  const name = authority.path.split(/[\\/]/).at(-1);
  if (authorityHashes.get(name) !== authority.sha256) {
    fail(`dispatch authority hash does not match docs/authority/manifest.sha256 for ${name}`);
  }
}
for (const field of ["allowed_read_paths", "allowed_write_paths", "global_invariants", "acceptance", "validation_commands", "budgets", "discretion", "stop_conditions", "receipt_destination"]) {
  if (!(field in dispatchPacket)) fail(`dispatch packet is missing ${field}`);
}

const evidenceSchema = JSON.parse(await readFile(join(root, "evidence.schema.json"), "utf8"));
for (const field of [
  "schema_version", "task_id", "run_id", "status", "acceptance_eligible", "reliability_runs",
  "scale_session_runs", "handle_checkpoints", "owner_crash_probe", "global_failures", "limitations"
]) {
  if (!evidenceSchema.required?.includes(field)) fail(`evidence schema is missing required field ${field}`);
}

const solution = await readFile(join(root, "TerminalProof.slnx"), "utf8");
const solutionProjects = [...solution.matchAll(/<Project Path="([^"]+)"/g)].map(match => match[1]);
if (solutionProjects.length !== 5) fail(`TerminalProof.slnx must contain five projects, found ${solutionProjects.length}`);
for (const projectPath of solutionProjects) await requireFile(projectPath);

async function walk(directory) {
  const results = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (["bin", "obj", "evidence"].includes(entry.name)) continue;
    const path = join(directory, entry.name);
    if (entry.isDirectory()) results.push(...await walk(path));
    else results.push(path);
  }
  return results;
}

const allFiles = await walk(root);
const projectFiles = allFiles.filter(path => path.endsWith(".csproj"));
for (const projectFile of projectFiles) {
  const text = await readFile(projectFile, "utf8");
  for (const match of text.matchAll(/<ProjectReference Include="([^"]+)"/g)) {
    const target = resolve(dirname(projectFile), match[1].replaceAll("\\", "/"));
    try {
      if (!(await stat(target)).isFile()) fail(`${relative(root, projectFile)} references missing ${match[1]}`);
    } catch {
      fail(`${relative(root, projectFile)} references missing ${match[1]}`);
    }
  }
  if (/PackageReference/.test(text)) fail(`${relative(root, projectFile)} adds an unreviewed package dependency`);
  if (/(?:^|[\\/])src[\\/]/i.test(text)) fail(`${relative(root, projectFile)} references production source`);
}

const sources = allFiles.filter(path => path.endsWith(".cs"));
for (const source of sources) {
  const text = await readFile(source, "utf8");
  if (/\.\.[\\/].*src[\\/]/i.test(text)) fail(`${relative(root, source)} reaches production source`);
}

const productionFiles = await walk(join(repositoryRoot, "src"));
for (const productionFile of productionFiles.filter(path => path.endsWith(".csproj") || path.endsWith(".cs"))) {
  const text = await readFile(productionFile, "utf8");
  if (/TerminalProof|prototypes[\\/]TerminalProof/i.test(text)) {
    fail(`${relative(repositoryRoot, productionFile)} references the isolated TerminalProof prototype`);
  }
}

const nativeMethods = await readFile(join(root, "Square.TerminalProof.Native/NativeMethods.cs"), "utf8");
for (const marker of [
  "CreatePseudoConsole", "ResizePseudoConsole", "ClosePseudoConsole", "CreateSuspended",
  "AssignProcessToJobObject", "TerminateJobObject", "QueryInformationJobObject", "WaitForSingleObject"
]) {
  if (!nativeMethods.includes(marker)) fail(`native interop is missing ${marker}`);
}

const attributeSource = await readFile(join(root, "Square.TerminalProof.Native/ProcThreadAttributeList.cs"), "utf8");
if (!attributeSource.includes("pseudoConsole,\n                    (nuint)nint.Size")) {
  fail("pseudoconsole startup attribute must pass the HPCON value directly");
}
if (attributeSource.includes("Marshal.WriteIntPtr")) {
  fail("pseudoconsole startup attribute must not pass a pointer-to-HPCON cell");
}

const sessionSource = await readFile(join(root, "Square.TerminalProof.Native/ConPtyTerminalSession.cs"), "utf8");
for (const marker of ["TaskCreationOptions.LongRunning", "PumpOutput", "SendCtrlCAsync", "HardStopAsync", "GetActiveProcessIds", "GetAccounting", "ShutdownAsync", "inheritHandles: false"]) {
  if (!sessionSource.includes(marker)) fail(`terminal session is missing ${marker}`);
}
if (sessionSource.includes("STARTF_USESTDHANDLES") && !sessionSource.includes("Do not set STARTF_USESTDHANDLES")) {
  fail("terminal session must not override ConPTY standard handles");
}
if (/Flags\s*=\s*NativeMethods\.[A-Za-z]*StdHandles/.test(sessionSource)) {
  fail("terminal session must not set STARTF_USESTDHANDLES for the hosted process");
}
const createIndex = sessionSource.indexOf("CreateProcessW(");
const assignIndex = sessionSource.indexOf("job.Assign(processHandle)");
const resumeIndex = sessionSource.indexOf("ResumeThread(threadHandle)");
if (!(createIndex >= 0 && createIndex < assignIndex && assignIndex < resumeIndex)) {
  fail("root process must be created suspended, assigned to the Job Object, and only then resumed");
}

const fixtureSource = await readFile(join(root, "Square.TerminalProof.Fixture/FixtureProgram.cs"), "utf8");
for (const scenario of expectedScenarios) {
  if (!fixtureSource.includes(`"${scenario}"`)) fail(`fixture source is missing scenario ${scenario}`);
}
for (const marker of ["UNICODE:café", "ANSI-RED", "BURST-BEGIN", "QUIET-READY", "QUESTION:enter-square-proof-token>", "RESIZE-READY", "CANCEL-ACK", "FORCE-CANCEL-IGNORED", "TREE-READY"]) {
  if (!fixtureSource.includes(marker)) fail(`fixture source is missing marker ${marker}`);
}

const ownerSource = await readFile(join(root, "Square.TerminalProof.CrashOwner/Program.cs"), "utf8");
for (const marker of ["CaptureProcessIdentity", "StartTimeUtcTicks", "CrashOwnerProcess", "JsonNamingPolicy.SnakeCaseLower"]) {
  if (!ownerSource.includes(marker)) fail(`owner-crash probe is missing ${marker}`);
}

const environmentSource = await readFile(join(root, "Square.TerminalProof.Harness/ProofEnvironmentCollector.cs"), "utf8");
if (!environmentSource.includes("DispatchPacketSha256")) fail("environment evidence must hash the dispatch packet");

const sourceIdentity = await readFile(join(root, "Square.TerminalProof.Harness/ProofSourceIdentity.cs"), "utf8");
const manifestHash = createHash("sha256").update(await readFile(join(root, "scenarios.json"))).digest("hex");
const dispatchHash = createHash("sha256").update(await readFile(join(root, "dispatch.packet.json"))).digest("hex");
if (!sourceIdentity.includes(`CanonicalManifestSha256 = "${manifestHash}"`)) {
  fail("ProofSourceIdentity canonical manifest hash is stale");
}
if (!sourceIdentity.includes(`CanonicalDispatchPacketSha256 = "${dispatchHash}"`)) {
  fail("ProofSourceIdentity canonical dispatch packet hash is stale");
}

const runnerSource = await readFile(join(root, "Square.TerminalProof.Harness/ProofRunner.cs"), "utf8");
for (const marker of ["AcceptanceReliabilityRepetitions = 100", "ScaleRepeatEach", "SessionCounts.Order()", "OwnerCrashProbe.ExecuteAsync", "CanonicalManifestSha256", "CanonicalDispatchPacketSha256", "DIAGNOSTIC_PASS"]) {
  if (!runnerSource.includes(marker)) fail(`proof runner is missing contract marker ${marker}`);
}

const testsSource = await readFile(join(root, "Square.TerminalProof.Tests/Program.cs"), "utf8");
for (const marker of ["ManifestJsonRejectsUnknownFields", "OptionsRejectUnknownValue", "QuickModeRejectsRepeatOverride", "CanonicalScenarioShape"]) {
  if (!testsSource.includes(marker)) fail(`prototype contract tests are missing ${marker}`);
}

const runScript = await readFile(join(root, "run-proof.ps1"), "utf8");
if (!runScript.includes("TerminalProof.slnx")) fail("run-proof.ps1 must build the isolated TerminalProof solution");
if (!runScript.includes("Square.TerminalProof.Tests.exe")) fail("run-proof.ps1 must execute the prototype unit tests before the proof");
if (!runScript.includes("$EvidenceDirectory.harness.log")) fail("run-proof.ps1 must keep its console log outside the initially empty evidence directory");

console.log(
  `TerminalProof source-contract validation passed: ${sources.length} C# source files, ` +
  `${projectFiles.length} projects, ${manifest.scenarios.length} scenarios.`
);
