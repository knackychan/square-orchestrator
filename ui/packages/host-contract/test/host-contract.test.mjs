import test from "node:test";
import assert from "node:assert/strict";
import { HOST_CONTRACT_VERSION, parseHostMessage } from "../dist/index.js";

const versioned = (message) => ({ version: HOST_CONTRACT_VERSION, ...message });

test("known versioned messages validate", () => {
  assert.equal(parseHostMessage(versioned({ type: "terminal.resize", terminalId: "t", leaseId: "l", columns: 120, rows: 40 })).type, "terminal.resize");
});

test("unknown fields and message types fail closed", () => {
  assert.throws(() => parseHostMessage(versioned({ type: "host.shell", command: "rm" })), /Unknown/);
  assert.throws(() => parseHostMessage(versioned({ type: "host.copyText", text: "x", surprise: true })), /Unknown field/);
  assert.throws(() => parseHostMessage({ version: HOST_CONTRACT_VERSION, type: "toString" }), /Unknown/);
});

test("incompatible versions and missing required fields are rejected", () => {
  assert.throws(() => parseHostMessage({ version: "2.0", type: "host.copyText", text: "x" }), /Incompatible/);
  assert.throws(() => parseHostMessage(versioned({ type: "rpc.request", requestId: "r", method: "status" })), /params is required/);
});
