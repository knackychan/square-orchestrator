export const HOST_CONTRACT_VERSION = "1.0-draft" as const;

type VersionedHostMessage<TType extends string> = {
  readonly version: typeof HOST_CONTRACT_VERSION;
  readonly type: TType;
};

export type HostMessage =
  | (VersionedHostMessage<"rpc.request"> & { readonly requestId: string; readonly method: string; readonly params: unknown })
  | (VersionedHostMessage<"subscription.open"> & { readonly topic: string; readonly fromSequence: number })
  | (VersionedHostMessage<"terminal.input"> & { readonly terminalId: string; readonly leaseId: string; readonly data: string })
  | (VersionedHostMessage<"terminal.resize"> & { readonly terminalId: string; readonly leaseId: string; readonly columns: number; readonly rows: number })
  | (VersionedHostMessage<"host.openArtifact"> & { readonly artifactId: string })
  | (VersionedHostMessage<"host.copyText"> & { readonly text: string });

const fieldSets: Readonly<Record<HostMessage["type"], readonly string[]>> = Object.freeze({
  "rpc.request": ["version", "type", "requestId", "method", "params"],
  "subscription.open": ["version", "type", "topic", "fromSequence"],
  "terminal.input": ["version", "type", "terminalId", "leaseId", "data"],
  "terminal.resize": ["version", "type", "terminalId", "leaseId", "columns", "rows"],
  "host.openArtifact": ["version", "type", "artifactId"],
  "host.copyText": ["version", "type", "text"]
});

export function parseHostMessage(value: unknown): HostMessage {
  if (!isRecord(value) || typeof value.type !== "string" || !hasOwn(fieldSets, value.type)) {
    throw new Error("Unknown host message type");
  }
  if (value.version !== HOST_CONTRACT_VERSION) {
    throw new Error(`Incompatible host contract version '${String(value.version)}'`);
  }

  const type = value.type as HostMessage["type"];
  rejectUnknownFields(value, fieldSets[type]);
  switch (type) {
    case "rpc.request":
      requireString(value, "requestId");
      requireString(value, "method");
      requireField(value, "params");
      return value as HostMessage;
    case "subscription.open":
      requireString(value, "topic");
      requireNonNegativeInteger(value, "fromSequence");
      return value as HostMessage;
    case "terminal.input":
      requireString(value, "terminalId");
      requireString(value, "leaseId");
      requireString(value, "data", true);
      return value as HostMessage;
    case "terminal.resize":
      requireString(value, "terminalId");
      requireString(value, "leaseId");
      requirePositiveInteger(value, "columns");
      requirePositiveInteger(value, "rows");
      return value as HostMessage;
    case "host.openArtifact":
      requireString(value, "artifactId");
      return value as HostMessage;
    case "host.copyText":
      requireString(value, "text", true);
      return value as HostMessage;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function hasOwn(value: object, key: PropertyKey): boolean {
  return Object.prototype.hasOwnProperty.call(value, key);
}

function rejectUnknownFields(value: Record<string, unknown>, allowed: readonly string[]): void {
  for (const key of Object.keys(value)) {
    if (!allowed.includes(key)) throw new Error(`Unknown field '${key}' for ${String(value.type)}`);
  }
}

function requireField(value: Record<string, unknown>, key: string): void {
  if (!hasOwn(value, key)) throw new Error(`${key} is required`);
}

function requireString(value: Record<string, unknown>, key: string, allowEmpty = false): void {
  if (typeof value[key] !== "string" || (!allowEmpty && value[key].length === 0)) {
    throw new Error(`${key} must be ${allowEmpty ? "a string" : "a non-empty string"}`);
  }
}

function requireNonNegativeInteger(value: Record<string, unknown>, key: string): void {
  if (!Number.isSafeInteger(value[key]) || (value[key] as number) < 0) {
    throw new Error(`${key} must be a non-negative integer`);
  }
}

function requirePositiveInteger(value: Record<string, unknown>, key: string): void {
  if (!Number.isSafeInteger(value[key]) || (value[key] as number) <= 0) {
    throw new Error(`${key} must be a positive integer`);
  }
}
