import type { FitAddon } from "@xterm/addon-fit";
import type { IDisposable, Terminal } from "@xterm/xterm";
import type { SyntheticFrame } from "../shared/fixture.js";
import type { ProofControllerMode, ProofTerminalFixture, ProofTheme, UiToHostMessage } from "../shared/protocol.js";
import { BrowserRenderTimer } from "./browser-render-timer.js";
import { TerminalRenderQueue, type RenderQueueSnapshot } from "./render-queue.js";
import { xtermTheme, type XtermFactory } from "./xterm-runtime.js";

export class ProofTerminalPane {
  readonly #fixture: ProofTerminalFixture;
  readonly #root: HTMLElement;
  readonly #controllerLabel: HTMLButtonElement;
  readonly #terminalHost: HTMLElement;
  readonly #terminal: Terminal;
  readonly #fitAddon: FitAddon;
  readonly #queue: TerminalRenderQueue;
  readonly #inputSubscription: IDisposable;
  readonly #post: (message: UiToHostMessage) => void;
  #mode: ProofControllerMode;
  #visible = true;
  #scale = 1;

  constructor(
    fixture: ProofTerminalFixture,
    factory: XtermFactory,
    hiddenThrottleMs: number,
    maximumPendingBytes: number,
    post: (message: UiToHostMessage) => void
  ) {
    this.#fixture = fixture;
    this.#mode = fixture.controllerMode;
    this.#post = post;
    this.#root = document.createElement("section");
    this.#root.className = "terminal-pane";
    this.#root.dataset.terminalId = fixture.id;
    this.#root.tabIndex = 0;
    this.#root.setAttribute("role", "region");
    this.#root.setAttribute("aria-label", fixture.ariaLabel);

    const header = document.createElement("header");
    header.className = "terminal-pane__header";
    const identity = document.createElement("div");
    identity.className = "terminal-pane__identity";
    const title = document.createElement("strong");
    title.textContent = `${fixture.taskId} · ${fixture.title}`;
    const detail = document.createElement("span");
    detail.textContent = `${fixture.role} · ${fixture.route} · ${fixture.state}`;
    identity.append(title, detail);

    this.#controllerLabel = document.createElement("button");
    this.#controllerLabel.type = "button";
    this.#controllerLabel.className = "controller-indicator";
    this.#controllerLabel.addEventListener("click", () => this.requestController());
    this.#updateControllerLabel();
    header.append(identity, this.#controllerLabel);

    this.#terminalHost = document.createElement("div");
    this.#terminalHost.className = "terminal-pane__body";
    this.#terminalHost.setAttribute("aria-label", `${fixture.ariaLabel} output`);
    this.#root.append(header, this.#terminalHost);

    this.#terminal = new factory.Terminal({
      allowProposedApi: false,
      convertEol: true,
      cursorBlink: false,
      disableStdin: this.#mode !== "control",
      screenReaderMode: true,
      scrollback: 2_000,
      fontFamily: "Cascadia Mono, Consolas, monospace",
      fontSize: 12,
      lineHeight: 1.15,
      minimumContrastRatio: 4.5,
      theme: xtermTheme("dark")
    });
    this.#fitAddon = new factory.FitAddon();
    this.#terminal.loadAddon(this.#fitAddon);
    this.#terminal.open(this.#terminalHost);
    this.#inputSubscription = this.#terminal.onData((data) => {
      if (this.#mode !== "control") return;
      this.#post({
        version: "square.shared-ui-proof/1",
        type: "terminal.input",
        terminalId: this.#fixture.id,
        leaseId: "proof-controller-lease",
        data
      });
    });
    this.#queue = new TerminalRenderQueue(
      { write: (data, completed) => this.#terminal.write(data, completed) },
      new BrowserRenderTimer(),
      {
        hiddenDelayMs: hiddenThrottleMs,
        maximumPendingBytes,
        visibleDelayMs: 0,
        initiallyVisible: true
      }
    );
  }

  get id(): string { return this.#fixture.id; }
  get element(): HTMLElement { return this.#root; }
  get visible(): boolean { return this.#visible; }

  activate(): void {
    this.#fitAndReport();
  }

  enqueue(frames: readonly SyntheticFrame[]): void {
    for (const frame of frames) {
      const result = this.#queue.enqueue(frame);
      if (result.kind !== "accepted" && result.kind !== "duplicate") {
        throw new Error(`${this.#fixture.id} render queue rejected sequence ${frame.sequence}: ${result.kind}`);
      }
    }
  }

  setVisible(visible: boolean): void {
    this.#visible = visible;
    this.#root.hidden = !visible;
    this.#root.setAttribute("aria-hidden", visible ? "false" : "true");
    this.#queue.setVisible(visible);
    if (visible) requestAnimationFrame(() => this.#fitAndReport());
  }

  setScale(scale: number): void {
    this.#scale = scale;
    this.#terminal.options.fontSize = Math.max(10, 12 * scale);
    if (this.#visible) requestAnimationFrame(() => this.#fitAndReport());
  }

  setTheme(theme: ProofTheme): void {
    this.#terminal.options.theme = xtermTheme(theme);
  }

  setController(mode: ProofControllerMode): void {
    this.#mode = mode;
    this.#terminal.options.disableStdin = mode !== "control";
    this.#updateControllerLabel();
  }

  focus(): void {
    this.#root.focus();
    this.#terminal.focus();
  }

  containsFocus(): boolean {
    const active = document.activeElement;
    return active === this.#root || (active !== null && this.#root.contains(active));
  }

  requestController(): void {
    this.#post({
      version: "square.shared-ui-proof/1",
      type: "proof.controllerRequested",
      terminalId: this.#fixture.id
    });
  }

  snapshot(): RenderQueueSnapshot {
    return this.#queue.snapshot();
  }

  dispose(): void {
    this.#inputSubscription.dispose();
    this.#queue.dispose();
    this.#terminal.dispose();
    this.#root.remove();
  }

  #fitAndReport(): void {
    if (!this.#visible || !this.#root.isConnected) return;
    this.#fitAddon.fit();
    const columns = Math.max(1, this.#terminal.cols);
    const rows = Math.max(1, this.#terminal.rows);
    this.#post({
      version: "square.shared-ui-proof/1",
      type: "terminal.resize",
      terminalId: this.#fixture.id,
      leaseId: "proof-controller-lease",
      columns,
      rows
    });
  }

  #updateControllerLabel(): void {
    const text = this.#mode === "control" ? "CONTROL" : this.#mode === "view" ? "VIEW" : "CONTROLLED ELSEWHERE";
    this.#controllerLabel.textContent = text;
    this.#controllerLabel.dataset.mode = this.#mode;
    this.#controllerLabel.setAttribute("aria-label", `${this.#fixture.taskId} terminal controller: ${text}`);
    this.#controllerLabel.disabled = this.#mode === "control";
  }
}
