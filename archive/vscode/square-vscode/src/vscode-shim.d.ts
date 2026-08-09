declare module "vscode" {
  export interface Disposable { dispose(): unknown; }
  export interface ExtensionContext { subscriptions: { push(...items: Disposable[]): number }; }
  export namespace commands { function registerCommand(command: string, callback: (...args: unknown[]) => unknown): Disposable; }
  export namespace window { function showInformationMessage(message: string): Thenable<string | undefined>; }
}
