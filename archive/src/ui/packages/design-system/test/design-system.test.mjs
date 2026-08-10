import test from "node:test"; import assert from "node:assert/strict";
import { semanticTokens, statePresentations } from "../dist/index.js";
test("every state presentation has text and a non-color symbol", () => { for (const value of Object.values(statePresentations)) { assert.ok(value.text.length > 0); assert.ok(value.symbol.length > 0); assert.match(value.tone, /^(info|success|warning|danger)$/); } });
test("semantic tokens separate status from concrete colors", () => { for (const value of Object.values(semanticTokens)) { assert.match(value, /^var\(--square-/); assert.doesNotMatch(value, /#/); } });
