import { dirname, resolve } from "node:path";
import { mkdir, rename, rm, writeFile } from "node:fs/promises";
import { randomUUID } from "node:crypto";

export function parseOptions(argv, known) {
  const values = new Map();
  for (let index = 0; index < argv.length; index += 1) {
    const option = argv[index];
    if (!known.has(option)) throw new Error(`unknown option '${option}'`);
    if (index + 1 >= argv.length || argv[index + 1].startsWith("--")) throw new Error(`option '${option}' requires a value`);
    if (values.has(option)) throw new Error(`option '${option}' was specified more than once`);
    values.set(option, argv[++index]);
  }
  return values;
}

export function required(values, name) {
  const value = values.get(name);
  if (!value) throw new Error(`required option '${name}' was not supplied`);
  return value;
}

export function positiveInteger(values, name, fallback = undefined) {
  const text = values.get(name);
  if (text === undefined) {
    if (fallback === undefined) throw new Error(`required option '${name}' was not supplied`);
    return fallback;
  }
  const value = Number(text);
  if (!Number.isSafeInteger(value) || value <= 0) throw new Error(`option '${name}' must be a positive integer`);
  return value;
}

export async function writeJsonAtomic(path, value) {
  const fullPath = resolve(path);
  await mkdir(dirname(fullPath), { recursive: true });
  const temporary = `${fullPath}.${randomUUID()}.tmp`;
  try {
    await writeFile(temporary, `${JSON.stringify(value, null, 2)}\n`, { encoding: "utf8", flag: "wx" });
    await rename(temporary, fullPath);
  } finally {
    await rm(temporary, { force: true });
  }
}

export async function withTimeout(promise, milliseconds, description) {
  let timer;
  try {
    return await Promise.race([
      promise,
      new Promise((_, reject) => { timer = setTimeout(() => reject(new Error(`${description} timed out after ${milliseconds} ms`)), milliseconds); })
    ]);
  } finally {
    clearTimeout(timer);
  }
}
