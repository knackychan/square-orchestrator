type Thenable<T> = PromiseLike<T>;

declare module "vscode" {
  export const version: string;

  export interface Disposable { dispose(): void; }
  export interface Uri {
    readonly fsPath: string;
    toString(skipEncoding?: boolean): string;
  }
  export namespace Uri {
    function file(path: string): Uri;
    function joinPath(base: Uri, ...pathSegments: string[]): Uri;
  }

  export enum ViewColumn { One = 1 }

  export interface Webview {
    html: string;
    readonly cspSource: string;
    asWebviewUri(localResource: Uri): Uri;
    postMessage(message: unknown): Thenable<boolean>;
    onDidReceiveMessage(listener: (message: unknown) => unknown): Disposable;
  }

  export interface WebviewPanel extends Disposable {
    readonly webview: Webview;
    reveal(viewColumn?: ViewColumn, preserveFocus?: boolean): void;
    onDidDispose(listener: () => unknown): Disposable;
  }

  export interface ExtensionContext {
    readonly extensionUri: Uri;
    readonly globalStorageUri: Uri;
    readonly subscriptions: { push(...items: Disposable[]): number };
  }

  export const workspace: {
    readonly fs: {
      readFile(uri: Uri): Thenable<Uint8Array>;
      writeFile(uri: Uri, content: Uint8Array): Thenable<void>;
      rename(source: Uri, target: Uri, options?: { readonly overwrite?: boolean }): Thenable<void>;
      createDirectory(uri: Uri): Thenable<void>;
    };
  };

  export const commands: {
    registerCommand(command: string, callback: (...args: readonly unknown[]) => unknown): Disposable;
    executeCommand<T = unknown>(command: string, ...rest: readonly unknown[]): Thenable<T | undefined>;
  };

  export const window: {
    createWebviewPanel(
      viewType: string,
      title: string,
      showOptions: ViewColumn,
      options: {
        readonly enableScripts?: boolean;
        readonly retainContextWhenHidden?: boolean;
        readonly localResourceRoots?: readonly Uri[];
      }
    ): WebviewPanel;
    showInformationMessage(message: string): Thenable<string | undefined>;
    showErrorMessage(message: string): Thenable<string | undefined>;
  };
}
