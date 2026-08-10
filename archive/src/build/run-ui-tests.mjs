import { spawnSync } from "node:child_process";
import { walkFiles } from "./fs-walk.mjs";

const tests = [
  ...(await walkFiles("ui", (value) => value.endsWith(".test.mjs"))),
  ...(await walkFiles("vscode", (value) => value.endsWith(".test.mjs")))
];
const result = spawnSync(process.execPath, ["--test", ...tests], { stdio: "inherit" });
process.exit(result.status ?? 1);
