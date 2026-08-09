import { createHash } from "node:crypto";
import { readFile, readdir, writeFile } from "node:fs/promises";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const proofRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const manifestPath = join(proofRoot, "source-manifest.sha256");
const write = process.argv.includes("--write");
const files = [];
await walk(proofRoot, files);
const entries = [];
for (const path of files.sort()) {
  const relativePath = relative(proofRoot, path).replaceAll("\\", "/");
  const hash = createHash("sha256").update(await readFile(path)).digest("hex");
  entries.push(`${hash}  ${relativePath}`);
}
const expected = `${entries.join("\n")}\n`;

if (write) {
  await writeFile(manifestPath, expected, "utf8");
  console.log(`SharedUiProof source manifest written: ${entries.length} files.`);
  process.exit(0);
}

let actual;
try {
  actual = await readFile(manifestPath, "utf8");
} catch {
  console.error("SharedUiProof source manifest is missing. Run with --write after reviewing source changes.");
  process.exit(1);
}
if (actual !== expected) {
  console.error("SharedUiProof source manifest differs from the checked-in source set or hashes.");
  process.exit(1);
}
console.log(`SharedUiProof source manifest verified: ${entries.length} files.`);

async function walk(directory, output) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (["dist", "evidence", "bin", "obj", "node_modules"].includes(entry.name)) continue;
    if (entry.name === "source-manifest.sha256") continue;
    const path = join(directory, entry.name);
    if (entry.isDirectory()) await walk(path, output);
    else output.push(path);
  }
}
