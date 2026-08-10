import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import {
  SHARED_UI_PROOF_PROTOCOL,
  canonicalJson,
  parseBenchmark,
  parseFixture,
  parseHostToUiMessage,
  parseUiToHostMessage,
  sha256Hex
} from "../dist/src/shared/protocol.js";

const fixture = parseFixture(JSON.parse(await readFile(new URL("../fixtures/canonical-state.json", import.meta.url), "utf8")));
const benchmark = parseBenchmark(JSON.parse(await readFile(new URL("../fixtures/benchmark-manifest.json", import.meta.url), "utf8")));
const hash = await sha256Hex(fixture);

test("canonical JSON is independent of insertion order", () => {
  assert.equal(canonicalJson({ b: 2, a: { d: 4, c: 3 } }), '{"a":{"c":3,"d":4},"b":2}');
});

test("both production host kinds consume the same strict initialization state", () => {
  for (const host of ["webview2", "vscode"]) {
    const parsed = parseHostToUiMessage({
      version: SHARED_UI_PROOF_PROTOCOL,
      type: "proof.initialize",
      host,
      fixture,
      benchmark,
      expectedFixtureSha256: hash
    });
    assert.equal(parsed.host, host);
    assert.equal(parsed.fixture.fixtureId, "sp00-t04-canonical-workspace");
  }
});

test("unknown types, fields, and incompatible versions fail closed", () => {
  assert.throws(() => parseHostToUiMessage({ version: SHARED_UI_PROOF_PROTOCOL, type: "host.shell", command: "calc" }), /Unknown/);
  assert.throws(() => parseHostToUiMessage({ version: SHARED_UI_PROOF_PROTOCOL, type: "proof.setScale", scale: 1, extra: true }), /Unknown field/);
  assert.throws(() => parseUiToHostMessage({ version: "2", type: "proof.ready", host: "vscode" }), /Incompatible/);
});

test("terminal input has no arbitrary command field", () => {
  const valid = parseUiToHostMessage({
    version: SHARED_UI_PROOF_PROTOCOL,
    type: "terminal.input",
    terminalId: "terminal-01",
    leaseId: "lease-proof",
    data: "y\r"
  });
  assert.equal(valid.type, "terminal.input");
  assert.throws(() => parseUiToHostMessage({ ...valid, command: "powershell" }), /Unknown field/);
});
