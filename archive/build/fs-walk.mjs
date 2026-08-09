import { readdir } from "node:fs/promises";
import { join } from "node:path";

export async function walkFiles(root, predicate = () => true) {
  const results = [];
  async function visit(directory) {
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      if (["node_modules", "dist", "bin", "obj", ".git"].includes(entry.name)) continue;
      const full = join(directory, entry.name);
      if (entry.isDirectory()) await visit(full);
      else if (predicate(full)) results.push(full);
    }
  }
  await visit(root);
  return results.sort();
}
