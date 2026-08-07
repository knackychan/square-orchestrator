import { createHash } from "node:crypto";
import { readdir, readFile, stat } from "node:fs/promises";
import { dirname, join, normalize, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const SRC = join(ROOT, "src");

const allowed = new Map([
  ["Square.Domain", new Set()],
  ["Square.Contracts", new Set(["Square.Domain"])],
  ["Square.Application", new Set(["Square.Domain", "Square.Contracts"])],
  ["Square.Adapters.Abstractions", new Set(["Square.Domain", "Square.Contracts"])],
  ["Square.ControlPlane", new Set(["Square.Domain", "Square.Contracts", "Square.Application", "Square.Adapters.Abstractions"])],
  ["Square.Persistence.Sqlite", new Set(["Square.Domain", "Square.Contracts", "Square.Application"])],
  ["Square.Artifacts", new Set(["Square.Domain", "Square.Contracts", "Square.Application"])],
  ["Square.Platform.Windows", new Set(["Square.Domain", "Square.Contracts", "Square.Application", "Square.Adapters.Abstractions"])],
  ["Square.Adapters.CommandCode", new Set(["Square.Domain", "Square.Contracts", "Square.Adapters.Abstractions"])],
  ["Square.Adapters.OpenCode", new Set(["Square.Domain", "Square.Contracts", "Square.Adapters.Abstractions"])],
  ["Square.Adapters.Claude", new Set(["Square.Domain", "Square.Contracts", "Square.Adapters.Abstractions"])],
  ["Square.Adapters.Codex", new Set(["Square.Domain", "Square.Contracts", "Square.Adapters.Abstractions"])],
  // The M1 CLI is a non-persistent dry-run slice invoking application use cases only.
  // Persistence admission belongs to SP02-T01; the SP02 daemon split restores a thin RPC-only CLI boundary.
  ["Square.Cli", new Set(["Square.Domain", "Square.Contracts", "Square.Application"])],
  ["Square.Desktop", new Set(["Square.Contracts"])],
  ["Square.Daemon", new Set([
    "Square.Domain", "Square.Contracts", "Square.Application", "Square.ControlPlane",
    "Square.Persistence.Sqlite", "Square.Artifacts", "Square.Platform.Windows",
    "Square.Adapters.CommandCode", "Square.Adapters.OpenCode", "Square.Adapters.Claude", "Square.Adapters.Codex"
  ])]
]);

function fail(message) {
  throw new Error(message);
}

function projectNameFromInclude(include) {
  const normalized = include.replaceAll("\\", "/");
  const file = normalized.slice(normalized.lastIndexOf("/") + 1);
  return file.endsWith(".csproj") ? file.slice(0, -".csproj".length) : file;
}

async function filesRecursively(root, predicate) {
  const results = [];
  async function visit(directory) {
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      if ([".git", "bin", "obj", "dist", "node_modules"].includes(entry.name)) continue;
      const path = join(directory, entry.name);
      if (entry.isDirectory()) await visit(path);
      else if (predicate(path)) results.push(path);
    }
  }
  await visit(root);
  return results.sort();
}

const projectPaths = (await readdir(SRC, { withFileTypes: true }))
  .filter((entry) => entry.isDirectory())
  .map((entry) => join(SRC, entry.name, `${entry.name}.csproj`));

const projects = new Map();
const edges = [];
for (const projectPath of projectPaths) {
  try {
    await stat(projectPath);
  } catch {
    fail(`production module is missing its matching project file: ${relative(ROOT, projectPath)}`);
  }
  const name = projectNameFromInclude(projectPath);
  const text = await readFile(projectPath, "utf8");
  projects.set(name, projectPath);
  for (const match of text.matchAll(/<ProjectReference\s+Include="([^"]+)"\s*\/?\s*>/g)) {
    const include = match[1];
    if (include.toLowerCase().includes("prototypes")) fail(`production project ${name} references prototype path ${include}`);
    const target = projectNameFromInclude(include);
    edges.push([name, target]);
  }
}

const actualNames = [...projects.keys()].sort();
const expectedNames = [...allowed.keys()].sort();
if (JSON.stringify(actualNames) !== JSON.stringify(expectedNames)) {
  fail(`production project set differs from architecture map: actual=${actualNames.join(", ")} expected=${expectedNames.join(", ")}`);
}
for (const [source, target] of edges) {
  if (!projects.has(target)) fail(`${source} references missing production project ${target}`);
  if (!allowed.get(source)?.has(target)) fail(`${source} illegally references ${target}`);
}

const graph = new Map(actualNames.map((name) => [name, []]));
for (const [source, target] of edges) graph.get(source).push(target);
const visiting = new Set();
const visited = new Set();
function visit(node) {
  if (visiting.has(node)) fail(`dependency cycle reaches ${node}`);
  if (visited.has(node)) return;
  visiting.add(node);
  for (const target of graph.get(node)) visit(target);
  visiting.delete(node);
  visited.add(node);
}
for (const node of graph.keys()) visit(node);

const forbiddenNamespaces = [
  "Square.Platform", "Square.Persistence", "Square.Adapters", "Square.Daemon",
  "Square.Cli", "Square.Desktop", "System.Windows", "Microsoft.Web.WebView2"
];
for (const project of ["Square.Domain", "Square.Contracts", "Square.Application"]) {
  for (const sourcePath of await filesRecursively(join(SRC, project), (path) => path.endsWith(".cs"))) {
    const text = await readFile(sourcePath, "utf8");
    for (const forbidden of forbiddenNamespaces) {
      if (text.includes(`using ${forbidden}`)) fail(`${relative(ROOT, sourcePath)} imports forbidden namespace ${forbidden}`);
    }
  }
}

for (const solutionPath of await filesRecursively(ROOT, (path) => path.endsWith(".slnx"))) {
  const text = await readFile(solutionPath, "utf8");
  for (const match of text.matchAll(/<Project\s+Path="([^"]+)"\s*\/?\s*>/g)) {
    const projectPath = resolve(dirname(solutionPath), normalize(match[1].replaceAll("/", sep)));
    try {
      const info = await stat(projectPath);
      if (!info.isFile()) fail(`${relative(ROOT, solutionPath)} references non-file project ${match[1]}`);
    } catch {
      fail(`${relative(ROOT, solutionPath)} references missing project ${match[1]}`);
    }
  }
}

const authorityDirectory = join(ROOT, "docs", "authority");
const manifest = await readFile(join(authorityDirectory, "manifest.sha256"), "utf8");
for (const line of manifest.split(/\r?\n/).filter(Boolean)) {
  const separator = line.indexOf("  ");
  if (separator < 0) fail(`invalid authority manifest line: ${line}`);
  const expected = line.slice(0, separator);
  const filename = line.slice(separator + 2);
  const content = await readFile(join(authorityDirectory, filename));
  const actual = createHash("sha256").update(content).digest("hex");
  if (actual !== expected) fail(`authority hash mismatch for ${filename}`);
}

const globalJson = JSON.parse(await readFile(join(ROOT, "global.json"), "utf8"));
if (globalJson.sdk?.rollForward !== "disable") fail("global.json must disable SDK roll-forward");
const packageJson = JSON.parse(await readFile(join(ROOT, "package.json"), "utf8"));
const expectedNode = (await readFile(join(ROOT, ".nvmrc"), "utf8")).trim();
if (packageJson.engines?.node !== expectedNode) fail("package.json Node engine and .nvmrc differ");
if (packageJson.packageManager !== `pnpm@${packageJson.engines?.pnpm}`) fail("packageManager and pnpm engine differ");

console.log(`Repository verification passed: ${projects.size} production projects, ${edges.length} project references.`);
