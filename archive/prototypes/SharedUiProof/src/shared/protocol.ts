export const SHARED_UI_PROOF_PROTOCOL = "square.shared-ui-proof/1" as const;

export type ProofHostKind = "webview2" | "vscode" | "browser";
export type ProofTheme = "dark" | "light" | "high-contrast";
export type ProofLayoutPreset = "Operations" | "Focus Agent" | "Plan" | "Review" | "Resources";
export type ProofControllerMode = "view" | "control" | "controlled_elsewhere";

export interface ProofTerminalFixture {
  readonly id: string;
  readonly taskId: string;
  readonly title: string;
  readonly role: string;
  readonly route: string;
  readonly state: string;
  readonly controllerMode: ProofControllerMode;
  readonly ariaLabel: string;
}

export interface ProofFixtureState {
  readonly schemaVersion: "1.0";
  readonly fixtureId: string;
  readonly selectedTerminalId: string;
  readonly layoutPreset: ProofLayoutPreset;
  readonly terminals: readonly ProofTerminalFixture[];
}

export interface ProofBenchmarkManifest {
  readonly schemaVersion: "1.0";
  readonly runId: string;
  readonly terminalCounts: readonly number[];
  readonly themes: readonly ProofTheme[];
  readonly scales: readonly number[];
  readonly bytesPerTerminal: number;
  readonly frameBytes: number;
  readonly hiddenThrottleMs: number;
  readonly maximumPendingBytes: number;
  readonly maximumDurationMs: number;
}

type Versioned<TType extends string> = {
  readonly version: typeof SHARED_UI_PROOF_PROTOCOL;
  readonly type: TType;
};

export type HostToUiMessage =
  | (Versioned<"proof.initialize"> & {
      readonly host: ProofHostKind;
      readonly fixture: ProofFixtureState;
      readonly benchmark: ProofBenchmarkManifest;
      readonly expectedFixtureSha256: string;
    })
  | (Versioned<"proof.setLayout"> & { readonly preset: ProofLayoutPreset })
  | (Versioned<"proof.setTheme"> & { readonly theme: ProofTheme })
  | (Versioned<"proof.setScale"> & { readonly scale: number })
  | (Versioned<"proof.setController"> & { readonly terminalId: string; readonly mode: ProofControllerMode })
  | (Versioned<"proof.runBenchmark"> & { readonly runId: string });

export type UiToHostMessage =
  | (Versioned<"proof.ready"> & { readonly host: ProofHostKind })
  | (Versioned<"proof.result"> & { readonly runId: string; readonly fixtureSha256: string; readonly result: unknown })
  | (Versioned<"proof.error"> & { readonly code: string; readonly message: string })
  | (Versioned<"proof.layoutChanged"> & { readonly preset: ProofLayoutPreset; readonly selectedTerminalId: string })
  | (Versioned<"proof.controllerRequested"> & { readonly terminalId: string })
  | (Versioned<"terminal.input"> & { readonly terminalId: string; readonly leaseId: string; readonly data: string })
  | (Versioned<"terminal.resize"> & {
      readonly terminalId: string;
      readonly leaseId: string;
      readonly columns: number;
      readonly rows: number;
    });

const hosts = new Set<ProofHostKind>(["webview2", "vscode", "browser"]);
const themes = new Set<ProofTheme>(["dark", "light", "high-contrast"]);
const layouts = new Set<ProofLayoutPreset>(["Operations", "Focus Agent", "Plan", "Review", "Resources"]);
const controllers = new Set<ProofControllerMode>(["view", "control", "controlled_elsewhere"]);

const hostToUiFields: Readonly<Record<HostToUiMessage["type"], readonly string[]>> = Object.freeze({
  "proof.initialize": Object.freeze(["version", "type", "host", "fixture", "benchmark", "expectedFixtureSha256"]),
  "proof.setLayout": Object.freeze(["version", "type", "preset"]),
  "proof.setTheme": Object.freeze(["version", "type", "theme"]),
  "proof.setScale": Object.freeze(["version", "type", "scale"]),
  "proof.setController": Object.freeze(["version", "type", "terminalId", "mode"]),
  "proof.runBenchmark": Object.freeze(["version", "type", "runId"])
});

const uiToHostFields: Readonly<Record<UiToHostMessage["type"], readonly string[]>> = Object.freeze({
  "proof.ready": Object.freeze(["version", "type", "host"]),
  "proof.result": Object.freeze(["version", "type", "runId", "fixtureSha256", "result"]),
  "proof.error": Object.freeze(["version", "type", "code", "message"]),
  "proof.layoutChanged": Object.freeze(["version", "type", "preset", "selectedTerminalId"]),
  "proof.controllerRequested": Object.freeze(["version", "type", "terminalId"]),
  "terminal.input": Object.freeze(["version", "type", "terminalId", "leaseId", "data"]),
  "terminal.resize": Object.freeze(["version", "type", "terminalId", "leaseId", "columns", "rows"])
});

export function parseHostToUiMessage(value: unknown): HostToUiMessage {
  const record = requireVersionedRecord(value, hostToUiFields, "host-to-UI");
  const type = record.type as HostToUiMessage["type"];
  rejectUnknownFields(record, hostToUiFields[type]);
  switch (type) {
    case "proof.initialize":
      requireEnum(record, "host", hosts);
      parseFixture(record.fixture);
      parseBenchmark(record.benchmark);
      requireSha256(record, "expectedFixtureSha256");
      return record as unknown as HostToUiMessage;
    case "proof.setLayout":
      requireEnum(record, "preset", layouts);
      return record as unknown as HostToUiMessage;
    case "proof.setTheme":
      requireEnum(record, "theme", themes);
      return record as unknown as HostToUiMessage;
    case "proof.setScale":
      requireScale(record.scale);
      return record as unknown as HostToUiMessage;
    case "proof.setController":
      requireNonEmptyString(record, "terminalId");
      requireEnum(record, "mode", controllers);
      return record as unknown as HostToUiMessage;
    case "proof.runBenchmark":
      requireNonEmptyString(record, "runId");
      return record as unknown as HostToUiMessage;
  }
}

export function parseUiToHostMessage(value: unknown): UiToHostMessage {
  const record = requireVersionedRecord(value, uiToHostFields, "UI-to-host");
  const type = record.type as UiToHostMessage["type"];
  rejectUnknownFields(record, uiToHostFields[type]);
  switch (type) {
    case "proof.ready":
      requireEnum(record, "host", hosts);
      return record as unknown as UiToHostMessage;
    case "proof.result":
      requireNonEmptyString(record, "runId");
      requireSha256(record, "fixtureSha256");
      requireOwn(record, "result");
      return record as unknown as UiToHostMessage;
    case "proof.error":
      requireNonEmptyString(record, "code");
      requireNonEmptyString(record, "message");
      return record as unknown as UiToHostMessage;
    case "proof.layoutChanged":
      requireEnum(record, "preset", layouts);
      requireNonEmptyString(record, "selectedTerminalId");
      return record as unknown as UiToHostMessage;
    case "proof.controllerRequested":
      requireNonEmptyString(record, "terminalId");
      return record as unknown as UiToHostMessage;
    case "terminal.input":
      requireNonEmptyString(record, "terminalId");
      requireNonEmptyString(record, "leaseId");
      requireString(record, "data");
      return record as unknown as UiToHostMessage;
    case "terminal.resize":
      requireNonEmptyString(record, "terminalId");
      requireNonEmptyString(record, "leaseId");
      requirePositiveIntegerField(record, "columns");
      requirePositiveIntegerField(record, "rows");
      return record as unknown as UiToHostMessage;
  }
}

export function parseFixture(value: unknown): ProofFixtureState {
  if (!isRecord(value)) throw new Error("fixture must be an object");
  rejectUnknownFields(value, ["schemaVersion", "fixtureId", "selectedTerminalId", "layoutPreset", "terminals"]);
  if (value.schemaVersion !== "1.0") throw new Error("fixture schemaVersion must be 1.0");
  requireNonEmptyString(value, "fixtureId");
  requireNonEmptyString(value, "selectedTerminalId");
  requireEnum(value, "layoutPreset", layouts);
  if (!Array.isArray(value.terminals) || value.terminals.length < 2 || value.terminals.length > 8) {
    throw new Error("fixture terminals must contain between two and eight terminals");
  }
  const ids = new Set<string>();
  for (const terminal of value.terminals) {
    if (!isRecord(terminal)) throw new Error("terminal fixture must be an object");
    rejectUnknownFields(terminal, ["id", "taskId", "title", "role", "route", "state", "controllerMode", "ariaLabel"]);
    for (const key of ["id", "taskId", "title", "role", "route", "state", "ariaLabel"] as const) {
      requireNonEmptyString(terminal, key);
    }
    requireEnum(terminal, "controllerMode", controllers);
    const id = terminal.id as string;
    if (ids.has(id)) throw new Error(`duplicate terminal id '${id}'`);
    ids.add(id);
  }
  if (!ids.has(value.selectedTerminalId as string)) throw new Error("selectedTerminalId must reference a terminal");
  return value as unknown as ProofFixtureState;
}

export function parseBenchmark(value: unknown): ProofBenchmarkManifest {
  if (!isRecord(value)) throw new Error("benchmark must be an object");
  rejectUnknownFields(value, [
    "schemaVersion", "runId", "terminalCounts", "themes", "scales", "bytesPerTerminal", "frameBytes",
    "hiddenThrottleMs", "maximumPendingBytes", "maximumDurationMs"
  ]);
  if (value.schemaVersion !== "1.0") throw new Error("benchmark schemaVersion must be 1.0");
  requireNonEmptyString(value, "runId");
  requireNumberArray(value, "terminalCounts", (entry) => Number.isSafeInteger(entry) && entry >= 1 && entry <= 8);
  requireEnumArray(value, "themes", themes);
  requireNumberArray(value, "scales", (entry) => Number.isFinite(entry) && entry >= 1 && entry <= 2);
  for (const key of ["bytesPerTerminal", "frameBytes", "hiddenThrottleMs", "maximumPendingBytes", "maximumDurationMs"] as const) {
    requirePositiveIntegerField(value, key);
  }
  if ((value.frameBytes as number) > (value.maximumPendingBytes as number)) {
    throw new Error("frameBytes cannot exceed maximumPendingBytes");
  }
  return value as unknown as ProofBenchmarkManifest;
}

export function canonicalJson(value: unknown): string {
  return JSON.stringify(canonicalize(value));
}

export async function sha256Hex(value: unknown): Promise<string> {
  const bytes = new TextEncoder().encode(canonicalJson(value));
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  return [...new Uint8Array(digest)].map((part) => part.toString(16).padStart(2, "0")).join("");
}

function canonicalize(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(canonicalize);
  if (!isRecord(value)) return value;
  const result: Record<string, unknown> = {};
  for (const key of Object.keys(value).sort()) result[key] = canonicalize(value[key]);
  return result;
}

function requireVersionedRecord(
  value: unknown,
  fieldMap: Readonly<Record<string, readonly string[]>>,
  direction: string
): Record<string, unknown> {
  if (!isRecord(value) || typeof value.type !== "string" || !Object.prototype.hasOwnProperty.call(fieldMap, value.type)) {
    throw new Error(`Unknown ${direction} message type`);
  }
  if (value.version !== SHARED_UI_PROOF_PROTOCOL) {
    throw new Error(`Incompatible shared UI proof protocol '${String(value.version)}'`);
  }
  return value;
}

function rejectUnknownFields(record: Record<string, unknown>, allowed: readonly string[]): void {
  const allow = new Set(allowed);
  for (const key of Object.keys(record)) {
    if (!allow.has(key)) throw new Error(`Unknown field '${key}'`);
  }
  for (const key of allowed) {
    if (!Object.prototype.hasOwnProperty.call(record, key)) throw new Error(`Missing field '${key}'`);
  }
}

function requireOwn(record: Record<string, unknown>, key: string): void {
  if (!Object.prototype.hasOwnProperty.call(record, key)) throw new Error(`Missing field '${key}'`);
}

function requireString(record: Record<string, unknown>, key: string): void {
  if (typeof record[key] !== "string") throw new Error(`${key} must be a string`);
}

function requireNonEmptyString(record: Record<string, unknown>, key: string): void {
  if (typeof record[key] !== "string" || record[key].trim().length === 0) throw new Error(`${key} must be a non-empty string`);
}

function requirePositiveIntegerField(record: Record<string, unknown>, key: string): void {
  const value = record[key];
  if (typeof value !== "number" || !Number.isSafeInteger(value) || value <= 0) throw new Error(`${key} must be a positive integer`);
}

function requireSha256(record: Record<string, unknown>, key: string): void {
  const value = record[key];
  if (typeof value !== "string" || !/^[0-9a-f]{64}$/.test(value)) throw new Error(`${key} must be a lowercase SHA-256 value`);
}

function requireScale(value: unknown): void {
  if (typeof value !== "number" || !Number.isFinite(value) || value < 1 || value > 2) {
    throw new Error("scale must be between 1 and 2");
  }
}

function requireNumberArray(record: Record<string, unknown>, key: string, predicate: (entry: number) => boolean): void {
  const value = record[key];
  if (!Array.isArray(value) || value.length === 0 || value.some((entry) => typeof entry !== "number" || !predicate(entry))) {
    throw new Error(`${key} must be a non-empty valid number array`);
  }
}

function requireEnumArray<T extends string>(record: Record<string, unknown>, key: string, values: ReadonlySet<T>): void {
  const value = record[key];
  if (!Array.isArray(value) || value.length === 0 || value.some((entry) => typeof entry !== "string" || !values.has(entry as T))) {
    throw new Error(`${key} contains an unsupported value`);
  }
}

function requireEnum<T extends string>(record: Record<string, unknown>, key: string, values: ReadonlySet<T>): void {
  const value = record[key];
  if (typeof value !== "string" || !values.has(value as T)) throw new Error(`${key} contains an unsupported value`);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
