import { spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { cp, mkdir, readFile, readdir, rm, writeFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const proofRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const repositoryRoot = resolve(proofRoot, "../..");
const dist = join(proofRoot, "dist");
const cleanOnly = process.argv.includes("--clean");
const skipVendor = process.argv.includes("--skip-vendor");

await rm(dist, { recursive: true, force: true });
if (cleanOnly) process.exit(0);

const npx = process.platform === "win32" ? "npx.cmd" : "npx";
const compile = spawnSync(npx, ["tsc", "-p", join(proofRoot, "tsconfig.json"), "--pretty", "false"], {
  cwd: repositoryRoot,
  encoding: "utf8",
  stdio: "pipe"
});
if (compile.status !== 0) {
  process.stderr.write(compile.stdout ?? "");
  process.stderr.write(compile.stderr ?? "");
  process.exit(compile.status ?? 1);
}

await cp(join(proofRoot, "web", "index.template.html"), join(dist, "index.template.html"));
await cp(join(proofRoot, "web", "styles.css"), join(dist, "styles.css"));
await mkdir(join(dist, "fixtures"), { recursive: true });
for (const name of ["canonical-state.json", "canonical-state.sha256", "benchmark-manifest.json"]) {
  await cp(join(proofRoot, "fixtures", name), join(dist, "fixtures", name));
}
await cp(join(proofRoot, "bridge-contract.json"), join(dist, "bridge-contract.json"));
await cp(join(proofRoot, "scenario-manifest.json"), join(dist, "scenario-manifest.json"));

if (skipVendor) {
  await mkdir(join(dist, "vendor"), { recursive: true });
  await writeFile(
    join(dist, "vendor", "NOT_RESTORED.txt"),
    "Host-neutral validation omitted vendor bytes. Run corepack pnpm install --frozen-lockfile and the normal build before launching either host.\n",
    "utf8"
  );
} else {
  await copyVendorAssets();
}

await writeFile(join(dist, "asset-manifest.sha256"), await hashManifest(dist), "utf8");
console.log(`SharedUiProof build completed (${skipVendor ? "host-neutral; vendor omitted" : "launchable assets"}).`);

async function copyVendorAssets() {
  const nodeModules = join(repositoryRoot, "node_modules");
  const assets = [
    [join(nodeModules, "@xterm", "xterm", "lib", "xterm.mjs"), join(dist, "vendor", "xterm.mjs")],
    [join(nodeModules, "@xterm", "xterm", "css", "xterm.css"), join(dist, "vendor", "xterm.css")],
    [join(nodeModules, "@xterm", "xterm", "LICENSE"), join(dist, "vendor", "XTERM-LICENSE.txt")],
    [join(nodeModules, "@xterm", "addon-fit", "lib", "addon-fit.mjs"), join(dist, "vendor", "addon-fit.mjs")],
    [join(nodeModules, "@xterm", "addon-fit", "LICENSE"), join(dist, "vendor", "ADDON-FIT-LICENSE.txt")]
  ];
  await mkdir(join(dist, "vendor"), { recursive: true });
  for (const [source, destination] of assets) {
    try {
      await cp(source, destination);
    } catch (error) {
      throw new Error(`Missing pinned vendor asset '${source}'. Run corepack pnpm install --frozen-lockfile.`, { cause: error });
    }
  }
}

async function hashManifest(root) {
  const files = [];
  await walk(root, root, files);
  const lines = [];
  for (const relative of files.sort()) {
    if (relative === "asset-manifest.sha256") continue;
    const bytes = await readFile(join(root, relative));
    lines.push(`${createHash("sha256").update(bytes).digest("hex")}  ${relative.replaceAll("\\", "/")}`);
  }
  return `${lines.join("\n")}\n`;
}

async function walk(root, directory, output) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) await walk(root, path, output);
    else output.push(path.slice(root.length + 1));
  }
}
