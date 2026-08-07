import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const template = await readFile(new URL("../web/index.template.html", import.meta.url), "utf8");
const extension = await readFile(new URL("../src/vscode/extension.ts", import.meta.url), "utf8");
const bridge = JSON.parse(await readFile(new URL("../bridge-contract.json", import.meta.url), "utf8"));

test("the shared HTML template carries a nonce CSP and local-only import map", () => {
  assert.match(template, /Content-Security-Policy/);
  assert.match(template, /nonce="\{\{NONCE\}\}" type="importmap"/);
  assert.doesNotMatch(template, /https?:\/\//);
});

test("the extension host validates messages and never executes terminal data as a command", () => {
  assert.match(extension, /parseUiToHostMessage/);
  assert.match(extension, /case "terminal\.input"/);
  assert.match(extension, /style-src-attr 'unsafe-inline'/);
  assert.doesNotMatch(extension, /`style-src\s+[^`\n]*unsafe-inline/);
  assert.doesNotMatch(extension, /child_process|exec\(|spawn\(|createTerminal\(/);
});

test("the bridge catalogue has no arbitrary shell message", () => {
  assert.equal(bridge.unknownTypes, "reject");
  assert.equal(bridge.unknownFields, "reject");
  assert.ok(!Object.keys(bridge.hostToUi).some((type) => type.includes("shell")));
  assert.ok(!Object.keys(bridge.uiToHost).some((type) => type.includes("shell")));
});
