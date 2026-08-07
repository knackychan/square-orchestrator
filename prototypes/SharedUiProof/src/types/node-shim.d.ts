declare module "node:crypto" {
  export function createHash(algorithm: string): {
    update(data: Uint8Array | string): { digest(encoding: "hex"): string };
  };
  export function randomBytes(size: number): { toString(encoding: "hex" | "base64url"): string };
}

declare module "node:path" {
  export function dirname(path: string): string;
}

declare const process: {
  readonly env: Readonly<Record<string, string | undefined>>;
  readonly version: string;
  readonly platform: string;
  readonly arch: string;
  readonly versions: Readonly<Record<string, string | undefined>>;
  memoryUsage(): Readonly<Record<string, number>>;
};
