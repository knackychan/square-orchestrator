interface VsCodeProofApi {
  postMessage(message: unknown): void;
  getState(): unknown;
  setState(state: unknown): void;
}

declare function acquireVsCodeApi(): VsCodeProofApi;

interface Window {
  readonly chrome?: {
    readonly webview?: {
      postMessage(message: unknown): void;
      addEventListener(type: "message", listener: (event: MessageEvent<unknown>) => void): void;
      removeEventListener(type: "message", listener: (event: MessageEvent<unknown>) => void): void;
    };
  };
}
