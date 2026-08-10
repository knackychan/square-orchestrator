import { createHash } from "node:crypto";
import { readdir, readFile, stat } from "node:fs/promises";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(root, "../..");

function fail(message) {
  throw new Error(message);
}

async function requireFile(relativePath) {
  const path = resolve(root, ...relativePath.split("/"));
  try {
    if (!(await stat(path)).isFile()) fail(`${relativePath} is not a file`);
  } catch {
    fail(`required file is missing: ${relativePath}`);
  }
  return path;
}

async function walk(directory) {
  const results = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (["bin", "obj", "evidence", "node_modules"].includes(entry.name)) continue;
    const path = join(directory, entry.name);
    if (entry.isDirectory()) results.push(...await walk(path));
    else results.push(path);
  }
  return results;
}

function sha256(content) {
  return createHash("sha256").update(content).digest("hex");
}

function stripCSharp(source) {
  let output = "";
  let index = 0;
  while (index < source.length) {
    const current = source[index];
    const next = source[index + 1];
    if (current === "/" && next === "/") {
      index += 2;
      while (index < source.length && source[index] !== "\n") index += 1;
      output += "\n";
      continue;
    }
    if (current === "/" && next === "*") {
      index += 2;
      while (index + 1 < source.length && !(source[index] === "*" && source[index + 1] === "/")) {
        output += source[index] === "\n" ? "\n" : " ";
        index += 1;
      }
      if (index + 1 >= source.length) fail("unterminated C# block comment");
      index += 2;
      continue;
    }
    if (current === '"') {
      const verbatim = index > 0 && source[index - 1] === "@";
      output += '"';
      index += 1;
      while (index < source.length) {
        if (verbatim && source[index] === '"' && source[index + 1] === '"') {
          index += 2;
          continue;
        }
        if (!verbatim && source[index] === "\\") {
          index += 2;
          continue;
        }
        if (source[index] === '"') {
          index += 1;
          output += '"';
          break;
        }
        output += source[index] === "\n" ? "\n" : " ";
        index += 1;
      }
      continue;
    }
    if (current === "'") {
      output += "'";
      index += 1;
      while (index < source.length) {
        if (source[index] === "\\") {
          index += 2;
          continue;
        }
        if (source[index] === "'") {
          index += 1;
          output += "'";
          break;
        }
        index += 1;
      }
      continue;
    }
    output += current;
    index += 1;
  }
  return output;
}

function validateBalancedCSharp(source, name) {
  const stripped = stripCSharp(source);
  const pairs = new Map([["}", "{"], [")", "("], ["]", "["]]);
  const stack = [];
  for (const character of stripped) {
    if (character === "{" || character === "(" || character === "[") stack.push(character);
    else if (pairs.has(character)) {
      if (stack.pop() !== pairs.get(character)) fail(`${name} has unbalanced '${character}'`);
    }
  }
  if (stack.length !== 0) fail(`${name} has unclosed delimiters: ${stack.join("")}`);
}

for (const path of [
  "PipeProof.slnx",
  "README.md",
  "dispatch.packet.json",
  "scenario-manifest.json",
  "evidence.schema.json",
  "protocol-vectors.json",
  "source-manifest.sha256",
  "run-proof.ps1",
  "node-client/frame-codec.mjs",
  "node-client/protocol.mjs",
  "node-client/named-pipe-client.mjs",
  "node-client/fixture.mjs",
  "node-client/reconnect-client.mjs"
]) await requireFile(path);

for (const repositoryRelativePath of [
  "docs/proofs/named-pipe-protocol.md",
  "docs/validation/sp00-t03-host-neutral-validation.txt",
  "docs/receipts/SP00-T03.prototype-receipt.json",
  "docs/IMPLEMENTATION_STATUS.md",
  "prototypes/SquareOrchestrator.Prototypes.slnx"
]) {
  const path = resolve(repositoryRoot, ...repositoryRelativePath.split("/"));
  try {
    if (!(await stat(path)).isFile()) fail(`${repositoryRelativePath} is not a file`);
  } catch {
    fail(`required repository output is missing: ${repositoryRelativePath}`);
  }
}

const manifestBytes = await readFile(join(root, "scenario-manifest.json"));
const manifestHash = sha256(manifestBytes);
const manifest = JSON.parse(manifestBytes);
const expectedScenarios = [
  "acl-security",
  "cross-language-parity",
  "version-negotiation",
  "framing-failures",
  "disconnect-replay",
  "daemon-restart-reconnect",
  "slow-subscriber",
  "replay-window-refusal",
  "graceful-shutdown"
];
if (manifest.schema_version !== "1.0" || manifest.task_id !== "SP00-T03") {
  fail("scenario-manifest.json must identify SP00-T03 schema 1.0");
}
if (JSON.stringify(manifest.scenarios.map(scenario => scenario.id)) !== JSON.stringify(expectedScenarios)) {
  fail(`scenario order must remain: ${expectedScenarios.join(", ")}`);
}
if (manifest.scenarios.filter(scenario => scenario.required_in_quick_mode).length !== 4) {
  fail("exactly four scenarios must be required in quick mode");
}

const dispatchBytes = await readFile(join(root, "dispatch.packet.json"));
const dispatchHash = sha256(dispatchBytes);
const dispatch = JSON.parse(dispatchBytes);
if (dispatch.schema_version !== "1.0" || dispatch.task_id !== "SP00-T03") {
  fail("dispatch.packet.json must identify SP00-T03 schema 1.0");
}
for (const field of [
  "authority_documents", "allowed_read_paths", "allowed_write_paths", "global_invariants",
  "acceptance", "validation_commands", "budgets", "discretion", "stop_conditions", "receipt_destination"
]) {
  if (!(field in dispatch)) fail(`dispatch packet is missing ${field}`);
}
const authorityManifest = await readFile(join(repositoryRoot, "docs/authority/manifest.sha256"), "utf8");
const authorityHashes = new Map(authorityManifest.trim().split(/\r?\n/).map(line => {
  const separator = line.indexOf("  ");
  return [line.slice(separator + 2), line.slice(0, separator)];
}));
for (const authority of dispatch.authority_documents) {
  const filename = authority.path.split(/[\\/]/).at(-1);
  if (authorityHashes.get(filename) !== authority.sha256) {
    fail(`dispatch authority hash is stale for ${filename}`);
  }
}
for (const requiredWrite of [
  "prototypes/PipeProof/",
  "docs/proofs/named-pipe-protocol.md",
  "docs/validation/sp00-t03-host-neutral-validation.txt",
  "docs/receipts/SP00-T03.prototype-receipt.json"
]) {
  if (!dispatch.allowed_write_paths.includes(requiredWrite)) fail(`dispatch write set is missing ${requiredWrite}`);
}
for (const budget of [
  "maximum_frame_payload_bytes", "control_queue_capacity", "event_queue_capacity",
  "subscription_queue_capacity", "journal_retention_events", "maximum_replay_events_per_subscribe"
]) {
  if (!Number.isSafeInteger(dispatch.budgets[budget]) || dispatch.budgets[budget] <= 0) {
    fail(`dispatch budget ${budget} must be a positive integer`);
  }
}

const evidenceSchema = JSON.parse(await readFile(join(root, "evidence.schema.json"), "utf8"));
if (evidenceSchema.$schema !== "https://json-schema.org/draft/2020-12/schema") {
  fail("evidence.schema.json must use JSON Schema Draft 2020-12");
}
for (const field of [
  "schema_version", "task_id", "status", "acceptance_eligible", "ineligibility_reasons",
  "scenario_count", "passed_scenario_count", "failed_scenario_count", "dispatch_sha256",
  "scenario_manifest_sha256", "evidence_manifest_file", "scenario_ids", "conclusion"
]) {
  if (!evidenceSchema.required?.includes(field)) fail(`evidence schema is missing required field ${field}`);
}

const solution = await readFile(join(root, "PipeProof.slnx"), "utf8");
const solutionProjects = [...solution.matchAll(/<Project Path="([^"]+)"/g)].map(match => match[1]);
if (solutionProjects.length !== 8) fail(`PipeProof.slnx must contain eight projects, found ${solutionProjects.length}`);
for (const project of solutionProjects) await requireFile(project);

const allFiles = await walk(root);
const projectFiles = allFiles.filter(path => path.endsWith(".csproj"));
if (projectFiles.length !== 8) fail(`PipeProof must contain eight project files, found ${projectFiles.length}`);
for (const projectFile of projectFiles) {
  const text = await readFile(projectFile, "utf8");
  if (/PackageReference/.test(text)) fail(`${relative(root, projectFile)} adds an unreviewed package dependency`);
  if (/(?:^|[\\/])src[\\/]/i.test(text)) fail(`${relative(root, projectFile)} references production source`);
  for (const match of text.matchAll(/<ProjectReference Include="([^"]+)"/g)) {
    const target = resolve(dirname(projectFile), match[1].replaceAll("\\", "/"));
    try {
      if (!(await stat(target)).isFile()) fail(`${relative(root, projectFile)} references missing ${match[1]}`);
    } catch {
      fail(`${relative(root, projectFile)} references missing ${match[1]}`);
    }
  }
}

const sources = allFiles.filter(path => path.endsWith(".cs"));
if (sources.length < 45) fail(`expected at least 45 C# source files, found ${sources.length}`);
for (const source of sources) {
  const text = await readFile(source, "utf8");
  validateBalancedCSharp(text, relative(root, source));
  if (/\.\.[\\/].*src[\\/]/i.test(text)) fail(`${relative(root, source)} reaches production source`);
}

const productionFiles = await walk(join(repositoryRoot, "src"));
for (const productionFile of productionFiles.filter(path => path.endsWith(".cs") || path.endsWith(".csproj"))) {
  const text = await readFile(productionFile, "utf8");
  if (/PipeProof|prototypes[\\/]PipeProof/i.test(text)) {
    fail(`${relative(repositoryRoot, productionFile)} references the isolated PipeProof prototype`);
  }
}

const fixtureDirectory = join(root, "fixtures/contracts");
const fixtureNames = (await readdir(fixtureDirectory)).filter(name => name.endsWith(".json")).sort();
if (fixtureNames.length !== 13) fail(`expected thirteen golden contract fixtures, found ${fixtureNames.length}`);
const vectors = JSON.parse(await readFile(join(root, "protocol-vectors.json"), "utf8"));
if (vectors.schema_version !== "1.0" || vectors.valid_messages.length !== 13 || vectors.invalid_messages.length < 10) {
  fail("protocol-vectors.json must contain 13 valid vectors and at least 10 invalid vectors");
}
for (let index = 0; index < fixtureNames.length; index += 1) {
  const fixture = JSON.parse(await readFile(join(fixtureDirectory, fixtureNames[index]), "utf8"));
  const vector = JSON.parse(vectors.valid_messages[index].json);
  if (JSON.stringify(fixture) !== JSON.stringify(vector)) {
    fail(`shared protocol vector differs from ${fixtureNames[index]}`);
  }
}

const protocolJson = await readFile(join(root, "Square.PipeProof.Protocol/ProtocolJson.cs"), "utf8");
for (const marker of ["SnakeCaseLower", "UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow", "RespectRequiredConstructorParameters = true"]) {
  if (!protocolJson.includes(marker)) fail(`strict protocol JSON options are missing ${marker}`);
}
const codec = await readFile(join(root, "Square.PipeProof.Protocol/LengthFramedJsonCodec.cs"), "utf8");
for (const marker of ["WriteUInt32BigEndian", "ReadUInt32BigEndian", "MaximumPayloadBytes", "_maximumWriteChunkBytes", "FrameSizeException", "allowCleanEndOfStream", "return null"]) {
  if (!codec.includes(marker)) fail(`length-framed codec is missing ${marker}`);
}
const messages = await readFile(join(root, "Square.PipeProof.Protocol/ProtocolMessages.cs"), "utf8");
for (const kind of ["HelloMessage", "HelloAckMessage", "RequestMessage", "ResponseMessage", "CancelMessage", "SubscribeMessage", "SubscribedMessage", "EventMessage", "SubscriptionClosedMessage", "ProtocolErrorMessage", "ServerGoingAwayMessage"]) {
  if (!messages.includes(kind)) fail(`protocol messages are missing ${kind}`);
}

const queue = await readFile(join(root, "Square.PipeProof.ServerCore/BoundedOutboundQueue.cs"), "utf8");
for (const marker of ["Channel.CreateBounded", "ControlQueueCapacity", "EventQueueCapacity", "TryEnqueueEvent", "WriteTimeoutMilliseconds", "ObserveQueueDepth", "BoundedChannelFullMode.Wait"]) {
  if (!queue.includes(marker)) fail(`bounded outbound queue is missing ${marker}`);
}
const eventHub = await readFile(join(root, "Square.PipeProof.ServerCore/EventHub.cs"), "utf8");
for (const marker of ["_publishGate", "MaximumReplayEvents", "SubscriptionQueueCapacity", "activate(registration", "ScheduleBackpressureNotification"]) {
  if (!eventHub.includes(marker)) fail(`event hub is missing ${marker}`);
}
const journal = await readFile(join(root, "Square.PipeProof.ServerCore/DurableEventJournal.cs"), "utf8");
for (const marker of ["FileOptions.WriteThrough", "Flush(flushToDisk: true)", "MinimumAvailableSequence", "ReplayUnavailableException", "throwOnInvalidBytes: true", "_retentionCapacity"]) {
  if (!journal.includes(marker)) fail(`durable journal is missing ${marker}`);
}
const connection = await readFile(join(root, "Square.PipeProof.ServerCore/ConnectionSession.cs"), "utf8");
for (const marker of ["HandshakeTimeout", "IncompatibleProtocol", "HandleCancelAsync", "MaximumInFlightRequests", "RegisterAsync", "ProtocolErrorCodes.RequestCancelled", "ServerGoingAwayMessage"]) {
  if (!connection.includes(marker)) fail(`connection session is missing ${marker}`);
}

const nativeMethods = await readFile(join(root, "Square.PipeProof.Transport.Windows/NativeMethods.cs"), "utf8");
for (const marker of ["CreateNamedPipeW", "PipeRejectRemoteClients", "GetSecurityInfo", "GetAce", "ImpersonateAnonymousToken", "CreateFileW"]) {
  if (!nativeMethods.includes(marker)) fail(`Windows transport interop is missing ${marker}`);
}
const pipeSecurity = await readFile(join(root, "Square.PipeProof.Transport.Windows/PipeSecurityDescriptor.cs"), "utf8");
for (const marker of ["D:P(A;;GA;;;", "S-1-5-18", "DaclProtected", "aces.Count == 2", "grantsOnlyExpected"]) {
  if (!pipeSecurity.includes(marker)) fail(`pipe security implementation is missing ${marker}`);
}
const listener = await readFile(join(root, "Square.PipeProof.Transport.Windows/WindowsNamedPipeListener.cs"), "utf8");
for (const marker of ["FileFlagFirstPipeInstance", "FileFlagOverlapped", "WaitForConnectionAsync", "GrantsOnlyCurrentUserAndSystem"]) {
  if (!listener.includes(marker)) fail(`Windows named-pipe listener is missing ${marker}`);
}

const nodeProtocol = await readFile(join(root, "node-client/protocol.mjs"), "utf8");
for (const marker of ["unknown field", "response must contain exactly one", "subscription_closed", "server_going_away", "EventSequenceTracker"]) {
  if (!nodeProtocol.includes(marker)) fail(`Node protocol validator is missing ${marker}`);
}
const nodeClient = await readFile(join(root, "node-client/named-pipe-client.mjs"), "utf8");
for (const marker of ["maximumWriteChunkBytes", "localSubscriptionCapacity", "performHandshake", "beginRequest", "subscribe", "writeChain"]) {
  if (!nodeClient.includes(marker)) fail(`Node named-pipe client is missing ${marker}`);
}
if (/webview|acquireVsCodeApi|window\.parent/i.test(nodeClient)) {
  fail("Node named-pipe client must remain outside and independent of webview code");
}

const sourceIdentity = await readFile(join(root, "Square.PipeProof.Harness/ProofSourceIdentity.cs"), "utf8");
if (!sourceIdentity.includes(`CanonicalDispatchPacketSha256 =
        "${dispatchHash}"`)) {
  fail("ProofSourceIdentity canonical dispatch hash is stale");
}
if (!sourceIdentity.includes(`CanonicalScenarioManifestSha256 =
        "${manifestHash}"`)) {
  fail("ProofSourceIdentity canonical scenario-manifest hash is stale");
}

const scenariosSource = await readFile(join(root, "Square.PipeProof.Harness/ProofScenarios.cs"), "utf8");
for (const marker of ["ExecuteAclSecurity", "ExecuteCrossLanguageParityAsync", "ExecuteVersionNegotiationAsync", "ExecuteFramingFailuresAsync", "ExecuteDisconnectReplayAsync", "ExecuteDaemonRestartReconnectAsync", "ExecuteSlowSubscriberAsync", "ExecuteReplayWindowRefusalAsync", "ExecuteGracefulShutdownAsync", "KillAsync(\"intentional daemon crash\""] ) {
  if (!scenariosSource.includes(marker)) fail(`proof scenarios are missing ${marker}`);
}
const serverProcess = await readFile(join(root, "Square.PipeProof.Harness/ServerProcess.cs"), "utf8");
for (const marker of ["--control-queue-capacity", "--event-queue-capacity", "--subscription-queue-capacity", "--journal-retention-capacity", "--maximum-replay-events", "KillAsync"]) {
  if (!serverProcess.includes(marker)) fail(`server process harness is missing ${marker}`);
}
const aggregateSolution = await readFile(
  join(repositoryRoot, "prototypes/SquareOrchestrator.Prototypes.slnx"),
  "utf8");
for (const project of solutionProjects) {
  const aggregatePath = `PipeProof/${project.replaceAll("\\", "/")}`;
  if (!aggregateSolution.includes(`Project Path="${aggregatePath}"`)) {
    fail(`aggregate prototype solution is missing ${aggregatePath}`);
  }
}

const receipt = JSON.parse(await readFile(
  join(repositoryRoot, "docs/receipts/SP00-T03.prototype-receipt.json"),
  "utf8"));
if (receipt.schema_version !== "1.0" || receipt.task_id !== "SP00-T03") {
  fail("SP00-T03 receipt identity is invalid");
}
if (receipt.implementation_status !== "SOURCE_IMPLEMENTED_WINDOWS_EXECUTION_PENDING"
    || receipt.architecture_proof_accepted !== false
    || receipt.g0_satisfied !== false) {
  fail("SP00-T03 receipt must not claim architecture acceptance");
}
if (receipt.dispatch_sha256 !== dispatchHash
    || receipt.scenario_manifest_sha256 !== manifestHash
    || receipt.protocol_vectors_sha256 !== sha256(await readFile(join(root, "protocol-vectors.json")))
    || receipt.source_validator_sha256 !== sha256(await readFile(join(root, "validate-source.mjs")))) {
  fail("SP00-T03 receipt source identities are stale");
}

const sourceManifestPath = join(root, "source-manifest.sha256");
const sourceManifestLines = (await readFile(sourceManifestPath, "utf8"))
  .split(/\r?\n/)
  .filter(Boolean);
const declaredSourceFiles = new Map(sourceManifestLines.map(line => {
  const separator = line.indexOf("  ");
  if (separator !== 64) fail(`invalid source-manifest line: ${line}`);
  return [line.slice(separator + 2), line.slice(0, separator)];
}));
const actualSourceFiles = (await walk(root))
  .filter(path => path !== sourceManifestPath)
  .sort((left, right) => left.localeCompare(right));
if (declaredSourceFiles.size !== actualSourceFiles.length) {
  fail(`source manifest file count differs: declared ${declaredSourceFiles.size}, actual ${actualSourceFiles.length}`);
}
for (const path of actualSourceFiles) {
  const name = relative(root, path).replaceAll("\\", "/");
  const declared = declaredSourceFiles.get(name);
  if (!declared) fail(`source manifest is missing ${name}`);
  const actual = sha256(await readFile(path));
  if (declared !== actual) fail(`source manifest hash is stale for ${name}`);
}

const runScript = await readFile(join(root, "run-proof.ps1"), "utf8");
for (const marker of ["validate-source.mjs", "PipeProof.slnx", "Square.PipeProof.Tests.csproj", "node-client\\fixture.mjs", "--dispatch", "--manifest"]) {
  if (!runScript.includes(marker)) fail(`run-proof.ps1 is missing ${marker}`);
}

console.log(
  `PipeProof source-contract validation passed: ${projectFiles.length} projects, ${sources.length} C# sources, ` +
  `${fixtureNames.length} contract fixtures, ${manifest.scenarios.length} Windows scenarios; ` +
  `dispatch ${sha256(dispatchBytes).slice(0, 12)}…`
);
