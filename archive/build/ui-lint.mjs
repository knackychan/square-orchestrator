import { readFile } from "node:fs/promises";
import { walkFiles } from "./fs-walk.mjs";

const files = await walkFiles("ui", (value) => value.endsWith(".ts"));
const forbidden = [
  { pattern: /#[0-9a-f]{3,8}\b/gi, reason: "fixed colors must not replace semantic tokens" },
  { pattern: /\b(?:window|document)\./g, reason: "shared packages must remain host-neutral" },
  { pattern: /child_process|execSync|spawnSync/g, reason: "shared UI cannot gain shell authority" }
];
for (const file of files) {
  const source = await readFile(file, "utf8");
  for (const rule of forbidden) {
    rule.pattern.lastIndex = 0;
    if (rule.pattern.test(source)) throw new Error(`${file}: ${rule.reason}`);
  }
}
console.log(`UI source policy checks passed (${files.length} source file(s)).`);
