import { parseHostToUiMessage, type HostToUiMessage, type ProofHostKind, type UiToHostMessage } from "../shared/protocol.js";

export interface ProofHostBridge {
  readonly host: ProofHostKind;
  post(message: UiToHostMessage): void;
  onMessage(listener: (message: HostToUiMessage) => void): () => void;
}

export function createProofHostBridge(): ProofHostBridge {
  if (window.chrome?.webview !== undefined) return createWebView2Bridge(window.chrome.webview);
  if (typeof acquireVsCodeApi === "function") return createVsCodeBridge(acquireVsCodeApi());
  return createBrowserBridge();
}

function createWebView2Bridge(webview: NonNullable<NonNullable<Window["chrome"]>["webview"]>): ProofHostBridge {
  return Object.freeze({
    host: "webview2" as const,
    post: (message: UiToHostMessage) => webview.postMessage(message),
    onMessage(listener: (message: HostToUiMessage) => void): () => void {
      const handler = (event: MessageEvent<unknown>): void => listener(parseHostToUiMessage(event.data));
      webview.addEventListener("message", handler);
      return () => webview.removeEventListener("message", handler);
    }
  });
}

function createVsCodeBridge(api: VsCodeProofApi): ProofHostBridge {
  return Object.freeze({
    host: "vscode" as const,
    post: (message: UiToHostMessage) => api.postMessage(message),
    onMessage(listener: (message: HostToUiMessage) => void): () => void {
      const handler = (event: MessageEvent<unknown>): void => listener(parseHostToUiMessage(event.data));
      window.addEventListener("message", handler);
      return () => window.removeEventListener("message", handler);
    }
  });
}

function createBrowserBridge(): ProofHostBridge {
  const listeners = new Set<(message: HostToUiMessage) => void>();
  window.addEventListener("message", (event: MessageEvent<unknown>) => {
    try {
      const message = parseHostToUiMessage(event.data);
      for (const listener of listeners) listener(message);
    } catch {
      // Direct browser mode is diagnostic only. Invalid messages remain rejected and have no authority.
    }
  });
  return Object.freeze({
    host: "browser" as const,
    post(message: UiToHostMessage): void {
      window.dispatchEvent(new CustomEvent("square-shared-ui-proof-output", { detail: message }));
    },
    onMessage(listener: (message: HostToUiMessage) => void): () => void {
      listeners.add(listener);
      return () => listeners.delete(listener);
    }
  });
}
