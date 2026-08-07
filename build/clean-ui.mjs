import { rm } from "node:fs/promises";
import { walkFiles } from "./fs-walk.mjs";

for (const path of await walkFiles(".", (value) => value.endsWith(".tsbuildinfo"))) {
  await rm(path, { force: true });
}
for (const directory of [
  "ui/packages/design-system/dist",
  "ui/packages/host-contract/dist",
  "ui/packages/terminal/dist",
  "ui/packages/workspace/dist",
  "vscode/square-vscode/dist",
  "prototypes/SharedUiProof/dist"
]) {
  await rm(directory, { recursive: true, force: true });
}
