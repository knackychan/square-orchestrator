import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, "..");
const gatePath = resolve(repositoryRoot, "docs/gates/G0-architecture-proof.json");
const gateReviewPath = resolve(repositoryRoot, "docs/gates/G0-architecture-proof-review.md");

function fail(message) {
  throw new Error(`SP00-T05 G0 validation failed: ${message}`);
}

function readUtf8(path) {
  if (!existsSync(path)) {
    fail(`missing file ${relative(repositoryRoot, path)}`);
  }
  return readFileSync(path, "utf8");
}

function sha256(path) {
  const hash = createHash("sha256");
  hash.update(readFileSync(path));
  return hash.digest("hex");
}

function assert(condition, message) {
  if (!condition) {
    fail(message);
  }
}

function verifyIdentity(identity, label) {
  assert(identity && typeof identity.path === "string", `${label} path is missing`);
  assert(/^[0-9a-f]{64}$/u.test(identity.sha256), `${label} SHA-256 is invalid`);
  const absolutePath = resolve(repositoryRoot, identity.path);
  assert(
    sha256(absolutePath) === identity.sha256,
    `${label} hash mismatch for ${identity.path}`,
  );
}

function walkFiles(root) {
  if (!existsSync(root)) {
    return [];
  }

  const files = [];
  for (const name of readdirSync(root)) {
    const path = resolve(root, name);
    const stat = statSync(path);
    if (stat.isDirectory()) {
      files.push(...walkFiles(path));
    } else if (stat.isFile()) {
      files.push(path);
    }
  }
  return files;
}

const gate = JSON.parse(readUtf8(gatePath));
assert(gate.schema_version === "1.0-draft", "unexpected gate schema version");
assert(gate.gate_id === "G0", "gate ID must be G0");
assert(gate.task_id === "SP00-T05", "task ID must be SP00-T05");
assert(gate.decision === "REJECTED_FOR_PROMOTION", "gate decision must fail closed");
assert(gate.gate_state === "BLOCKED", "gate state must be BLOCKED");
assert(gate.production_promotion_allowed === false, "production promotion must be denied");
assert(gate.sp01_dispatch_allowed === false, "SP01 dispatch must remain denied");
assert(gate.existing_sp01_drafts_authoritative === false, "SP01 drafts must remain non-authoritative");
assert(gate.prototype_evidence_retained === true, "prototype evidence retention must be explicit");

assert(Array.isArray(gate.authority_documents) && gate.authority_documents.length === 4, "four authority documents are required");
for (const [index, identity] of gate.authority_documents.entries()) {
  verifyIdentity(identity, `authority document ${index + 1}`);
}

const expectedProofIds = ["SP00-T02", "SP00-T03", "SP00-T04"];
assert(Array.isArray(gate.proofs) && gate.proofs.length === expectedProofIds.length, "three proof reviews are required");
for (const expectedId of expectedProofIds) {
  const proof = gate.proofs.find((candidate) => candidate.task_id === expectedId);
  assert(proof, `missing ${expectedId} proof review`);
  assert(proof.source_status === "IMPLEMENTED", `${expectedId} source status must be IMPLEMENTED`);
  assert(proof.host_neutral_validation === "PASS", `${expectedId} host-neutral result must be PASS`);
  assert(proof.empirical_windows_status === "MISSING", `${expectedId} Windows status must be MISSING`);
  assert(proof.acceptance_evidence_found === false, `${expectedId} cannot claim acceptance evidence`);
  assert(proof.decision === "REJECTED_FOR_PROMOTION", `${expectedId} decision must fail closed`);
  verifyIdentity(proof.proof_record, `${expectedId} proof record`);
  verifyIdentity(proof.receipt, `${expectedId} receipt`);
  verifyIdentity(proof.host_neutral_record, `${expectedId} validation record`);
  assert(Array.isArray(proof.canonical_inputs) && proof.canonical_inputs.length >= 2, `${expectedId} canonical inputs are incomplete`);
  for (const [index, identity] of proof.canonical_inputs.entries()) {
    verifyIdentity(identity, `${expectedId} canonical input ${index + 1}`);
  }
}

const expectedDecisionIds = ["ADR-0001", "ADR-0002", "ADR-0003", "ADR-0004", "ADR-0005"];
assert(Array.isArray(gate.architecture_decisions) && gate.architecture_decisions.length === expectedDecisionIds.length, "five ADRs are required");
for (const expectedId of expectedDecisionIds) {
  const decision = gate.architecture_decisions.find((candidate) => candidate.id === expectedId);
  assert(decision, `missing ${expectedId}`);
  assert(decision.status === "REJECTED_FOR_PROMOTION", `${expectedId} must be REJECTED_FOR_PROMOTION`);
  verifyIdentity(decision, expectedId);
  const content = readUtf8(resolve(repositoryRoot, decision.path));
  assert(content.includes("Status: `REJECTED_FOR_PROMOTION`"), `${expectedId} status marker is missing`);
  assert(content.includes("Evidence required to reconsider") || content.includes("Measurement required for a superseding decision"), `${expectedId} reconsideration evidence is missing`);
}

const review = readUtf8(gateReviewPath);
assert(review.includes("Decision: `REJECTED_FOR_PROMOTION`"), "human gate review decision is missing");
assert(review.includes("Gate state: `BLOCKED`"), "human gate review state is missing");
assert(review.includes("SP01 dispatch/finalization allowed: **No**"), "human gate review must block SP01");

const evidenceRoots = [
  resolve(repositoryRoot, "prototypes/TerminalProof/evidence"),
  resolve(repositoryRoot, "prototypes/PipeProof/evidence"),
  resolve(repositoryRoot, "prototypes/SharedUiProof/evidence"),
];
const ignoredEvidencePaths = new Set(
  (gate.acceptance_evidence_inventory?.ignored_placeholders ?? []).map((path) => resolve(repositoryRoot, path)),
);
const unreviewedEvidence = evidenceRoots
  .flatMap(walkFiles)
  .filter((path) => !ignoredEvidencePaths.has(path));
assert(unreviewedEvidence.length === 0, `unreviewed proof evidence exists: ${unreviewedEvidence.map((path) => relative(repositoryRoot, path)).join(", ")}`);
assert(gate.acceptance_evidence_inventory.reviewed_files.length === 0, "the gate must not claim reviewed acceptance files");
assert(gate.acceptance_evidence_inventory.unreviewed_acceptance_evidence_found === false, "the gate inventory must state no unreviewed evidence");

assert(gate.benchmark_decision.performance_thresholds_accepted === false, "unmeasured performance thresholds cannot be accepted");
assert(gate.benchmark_decision.correctness_prerequisites_retained === true, "correctness prerequisites must be retained");
assert(gate.benchmark_decision.proof_values_are_production_budgets === false, "proof values cannot be production budgets");

console.log("SP00-T05 G0 gate validation: PASS");
console.log("  reviewed proofs: 3");
console.log("  architecture decisions: 5");
console.log("  acceptance-eligible Windows evidence: 0");
console.log("  gate decision: REJECTED_FOR_PROMOTION");
console.log("  gate state: BLOCKED");
