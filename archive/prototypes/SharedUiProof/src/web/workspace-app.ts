import { createSyntheticFrames } from "../shared/fixture.js";
import type {
  ProofBenchmarkManifest,
  ProofControllerMode,
  ProofFixtureState,
  ProofLayoutPreset,
  ProofTheme,
  UiToHostMessage
} from "../shared/protocol.js";
import { ProofTerminalPane } from "./terminal-pane.js";
import type { XtermFactory } from "./xterm-runtime.js";

const layoutPresets: Readonly<Record<ProofLayoutPreset, readonly string[]>> = Object.freeze({
  Operations: Object.freeze(["Agent Fleet", "Selected Terminal", "Approvals", "Events"]),
  "Focus Agent": Object.freeze(["Selected Terminal"]),
  Plan: Object.freeze(["Task Graph", "Plan and Acceptance", "Evidence"]),
  Review: Object.freeze(["Diff", "Findings", "Acceptance"]),
  Resources: Object.freeze(["Agent Fleet", "Route Exposure", "Resource Health"])
});

const stateTones: Readonly<Record<string, string>> = Object.freeze({
  running: "info",
  quiet_active: "info",
  waiting_for_input: "warning",
  waiting_for_approval: "warning",
  auth_required: "danger",
  blocked: "danger",
  suspected_stall: "warning",
  succeeded: "success",
  failed: "danger"
});

export interface WorkspaceRenderSummary {
  readonly terminalId: string;
  readonly lastSequence: number;
  readonly renderedThroughSequence: number;
  readonly batchesWritten: number;
  readonly bytesWritten: number;
  readonly pendingFrames: number;
  readonly pendingBytes: number;
  readonly writing: boolean;
  readonly visible: boolean;
}

export class SharedUiProofWorkspace {
  readonly #root: HTMLElement;
  readonly #factory: XtermFactory;
  readonly #post: (message: UiToHostMessage) => void;
  readonly #toolbar = document.createElement("header");
  readonly #navigator = document.createElement("nav");
  readonly #canvas = document.createElement("main");
  readonly #inspector = document.createElement("aside");
  readonly #status = document.createElement("footer");
  #fixture: ProofFixtureState | null = null;
  #benchmark: ProofBenchmarkManifest | null = null;
  #panes: ProofTerminalPane[] = [];
  #selectedTerminalId = "";
  #layout: ProofLayoutPreset = "Operations";
  #theme: ProofTheme = "dark";
  #scale = 1;

  constructor(root: HTMLElement, factory: XtermFactory, post: (message: UiToHostMessage) => void) {
    this.#root = root;
    this.#factory = factory;
    this.#post = post;
    this.#buildShell();
  }

  initialize(fixture: ProofFixtureState, benchmark: ProofBenchmarkManifest): void {
    this.#fixture = fixture;
    this.#benchmark = benchmark;
    this.#selectedTerminalId = fixture.selectedTerminalId;
    this.#layout = fixture.layoutPreset;
    this.#renderNavigator();
    this.setTerminalCount(2);
    this.setTheme("dark");
    this.setScale(1);
    this.setLayout(fixture.layoutPreset, false);
    this.#root.setAttribute("aria-busy", "false");
    this.#status.textContent = `Fixture ${fixture.fixtureId} · ${fixture.terminals.length} recorded terminals · host-neutral state`;
  }

  setTerminalCount(count: number): void {
    const fixture = this.#requireFixture();
    const benchmark = this.#requireBenchmark();
    if (!Number.isSafeInteger(count) || count < 1 || count > fixture.terminals.length) throw new RangeError("terminal count is invalid");
    for (const pane of this.#panes) pane.dispose();
    this.#panes = [];
    this.#canvas.replaceChildren();
    this.#canvas.dataset.terminalCount = String(count);
    for (const terminal of fixture.terminals.slice(0, count)) {
      const pane = new ProofTerminalPane(
        terminal,
        this.#factory,
        benchmark.hiddenThrottleMs,
        benchmark.maximumPendingBytes,
        this.#post
      );
      pane.element.addEventListener("focusin", () => {
        this.#selectedTerminalId = pane.id;
        this.#renderInspector();
      });
      this.#panes.push(pane);
      this.#canvas.append(pane.element);
      pane.setTheme(this.#theme);
      pane.setScale(this.#scale);
      pane.activate();
    }
    if (!this.#panes.some((pane) => pane.id === this.#selectedTerminalId)) {
      this.#selectedTerminalId = this.#panes[0]?.id ?? "";
    }
    this.setLayout(this.#layout, false);
    this.#renderInspector();
  }

  setLayout(preset: ProofLayoutPreset, notify = true): void {
    if (!Object.prototype.hasOwnProperty.call(layoutPresets, preset)) throw new Error(`Unknown layout preset '${preset}'`);
    this.#layout = preset;
    this.#root.dataset.layout = preset.toLocaleLowerCase().replaceAll(" ", "-");
    const focusMode = preset === "Focus Agent";
    for (const pane of this.#panes) pane.setVisible(!focusMode || pane.id === this.#selectedTerminalId);
    this.#status.textContent = `${preset} · ${layoutPresets[preset].join(" · ")}`;
    this.#updateLayoutButtons();
    if (notify) {
      this.#post({
        version: "square.shared-ui-proof/1",
        type: "proof.layoutChanged",
        preset,
        selectedTerminalId: this.#selectedTerminalId
      });
    }
  }

  setTheme(theme: ProofTheme): void {
    this.#theme = theme;
    document.documentElement.dataset.theme = theme;
    this.#root.dataset.theme = theme;
    for (const pane of this.#panes) pane.setTheme(theme);
  }

  setScale(scale: number): void {
    if (!Number.isFinite(scale) || scale < 1 || scale > 2) throw new RangeError("scale must be between 1 and 2");
    this.#scale = scale;
    const key = scale === 1 ? "100" : scale === 1.5 ? "150" : scale === 2 ? "200" : String(Math.round(scale * 100));
    document.documentElement.dataset.scale = key;
    this.#root.dataset.scale = key;
    for (const pane of this.#panes) pane.setScale(scale);
  }

  setController(terminalId: string, mode: ProofControllerMode): void {
    this.#requirePane(terminalId).setController(mode);
  }

  setPaneVisible(terminalId: string, visible: boolean): void {
    this.#requirePane(terminalId).setVisible(visible);
  }

  renderSyntheticOutput(bytesPerTerminal: number, frameBytes: number): void {
    const fixtureById = new Map(this.#requireFixture().terminals.map((terminal) => [terminal.id, terminal]));
    for (const pane of this.#panes) {
      const fixture = fixtureById.get(pane.id);
      if (fixture === undefined) throw new Error(`Missing fixture for ${pane.id}`);
      pane.enqueue(createSyntheticFrames(fixture, bytesPerTerminal, frameBytes));
    }
  }

  async waitForRendered(timeoutMs: number, terminalIds?: ReadonlySet<string>): Promise<void> {
    const started = performance.now();
    while (true) {
      const candidates = terminalIds === undefined ? this.#panes : this.#panes.filter((pane) => terminalIds.has(pane.id));
      const complete = candidates.every((pane) => {
        const state = pane.snapshot();
        return state.lastSequence > 0
          && state.renderedThroughSequence === state.lastSequence
          && state.pendingFrames === 0
          && !state.writing;
      });
      if (complete) return;
      if (performance.now() - started > timeoutMs) throw new Error(`Terminal rendering exceeded ${timeoutMs}ms`);
      await new Promise<void>((resolve) => window.setTimeout(resolve, 10));
    }
  }

  snapshots(): readonly WorkspaceRenderSummary[] {
    return Object.freeze(this.#panes.map((pane) => {
      const snapshot = pane.snapshot();
      return Object.freeze({ terminalId: pane.id, ...snapshot });
    }));
  }

  focusTerminal(terminalId: string): void {
    this.#selectedTerminalId = terminalId;
    this.#requirePane(terminalId).focus();
    this.#renderInspector();
  }

  focusNext(): string {
    const visible = this.#panes.filter((pane) => pane.visible);
    if (visible.length === 0) throw new Error("No visible terminal can receive focus");
    const current = visible.findIndex((pane) => pane.id === this.#selectedTerminalId);
    const next = visible[(current + 1 + visible.length) % visible.length];
    if (next === undefined) throw new Error("Unable to select next terminal");
    this.focusTerminal(next.id);
    return next.id;
  }

  selectedTerminalId(): string { return this.#selectedTerminalId; }
  terminalIds(): readonly string[] { return Object.freeze(this.#panes.map((pane) => pane.id)); }
  terminalContainsFocus(terminalId: string): boolean { return this.#requirePane(terminalId).containsFocus(); }

  controllerText(terminalId: string): string {
    return this.#requirePane(terminalId).element.querySelector<HTMLElement>(".controller-indicator")?.textContent ?? "";
  }

  auditAccessibility(): readonly string[] {
    const failures: string[] = [];
    const regions = this.#canvas.querySelectorAll<HTMLElement>("[role='region']");
    if (regions.length !== this.#panes.length) failures.push("terminal region count differs from pane count");
    for (const pane of this.#panes) {
      const label = pane.element.getAttribute("aria-label") ?? "";
      if (label.length === 0) failures.push(`${pane.id} is missing an accessible region name`);
      const controller = pane.element.querySelector<HTMLButtonElement>(".controller-indicator");
      if ((controller?.getAttribute("aria-label") ?? "").length === 0) failures.push(`${pane.id} controller lacks an accessible name`);
      const output = pane.element.querySelector<HTMLElement>(".terminal-pane__body");
      if ((output?.getAttribute("aria-label") ?? "").length === 0) failures.push(`${pane.id} output lacks an accessible name`);
    }
    const labels = [...regions].map((region) => region.getAttribute("aria-label") ?? "");
    if (new Set(labels).size !== labels.length) failures.push("terminal accessible names are not unique");
    return Object.freeze(failures);
  }

  dispose(): void {
    for (const pane of this.#panes) pane.dispose();
    this.#panes = [];
    this.#root.replaceChildren();
  }

  #buildShell(): void {
    this.#root.className = "proof-workspace";
    this.#toolbar.className = "command-bar";
    const brand = document.createElement("div");
    brand.className = "command-bar__brand";
    const title = document.createElement("strong");
    title.textContent = "Square Orchestrator · Shared UI Proof";
    const subtitle = document.createElement("span");
    subtitle.textContent = "Same fixture and renderer in WebView2 and VS Code";
    brand.append(title, subtitle);
    const controls = document.createElement("div");
    controls.className = "layout-controls";
    for (const name of Object.keys(layoutPresets) as ProofLayoutPreset[]) {
      const button = document.createElement("button");
      button.type = "button";
      button.dataset.preset = name;
      button.textContent = name;
      button.setAttribute("aria-label", `Activate ${name} layout`);
      button.addEventListener("click", () => this.setLayout(name));
      controls.append(button);
    }
    this.#toolbar.append(brand, controls);

    this.#navigator.className = "navigator";
    this.#navigator.setAttribute("aria-label", "Recorded terminal fixtures");
    this.#canvas.className = "dock-canvas";
    this.#canvas.setAttribute("aria-label", "Terminal dock canvas");
    this.#inspector.className = "inspector";
    this.#inspector.setAttribute("aria-label", "Selected terminal inspector");
    this.#status.className = "status-strip";
    this.#status.setAttribute("role", "status");

    const shell = document.createElement("div");
    shell.className = "workspace-shell";
    shell.append(this.#navigator, this.#canvas, this.#inspector);
    this.#root.append(this.#toolbar, shell, this.#status);
    this.#root.addEventListener("keydown", (event) => {
      if (event.ctrlKey && event.key === "PageDown") {
        event.preventDefault();
        this.focusNext();
      }
      if (event.altKey && /^[1-5]$/.test(event.key)) {
        const preset = (Object.keys(layoutPresets) as ProofLayoutPreset[])[Number(event.key) - 1];
        if (preset !== undefined) {
          event.preventDefault();
          this.setLayout(preset);
        }
      }
    });
  }

  #renderNavigator(): void {
    const fixture = this.#requireFixture();
    const heading = document.createElement("h2");
    heading.textContent = "Agent terminals";
    const list = document.createElement("ol");
    for (const terminal of fixture.terminals) {
      const item = document.createElement("li");
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = `${terminal.taskId} · ${terminal.role}`;
      button.setAttribute("aria-label", `Focus ${terminal.ariaLabel}`);
      button.addEventListener("click", () => {
        if (this.#panes.some((pane) => pane.id === terminal.id)) this.focusTerminal(terminal.id);
      });
      const tone = stateTones[terminal.state.toLocaleLowerCase()];
      if (tone !== undefined) button.dataset.state = tone;
      item.append(button);
      list.append(item);
    }
    this.#navigator.replaceChildren(heading, list);
  }

  #renderInspector(): void {
    const terminal = this.#requireFixture().terminals.find((candidate) => candidate.id === this.#selectedTerminalId);
    if (terminal === undefined) {
      this.#inspector.textContent = "No terminal selected";
      return;
    }
    const heading = document.createElement("h2");
    heading.textContent = "Inspector";
    const rows = document.createElement("dl");
    const fields: readonly (readonly [string, string])[] = [
      ["Task", terminal.taskId],
      ["Role", terminal.role],
      ["Route", terminal.route],
      ["State", terminal.state],
      ["Authority", terminal.controllerMode.replaceAll("_", " ")],
      ["Fixture", this.#requireFixture().fixtureId]
    ];
    for (const [label, value] of fields) {
      const term = document.createElement("dt");
      term.textContent = label;
      const description = document.createElement("dd");
      description.textContent = value;
      rows.append(term, description);
    }
    this.#inspector.replaceChildren(heading, rows);
  }

  #updateLayoutButtons(): void {
    for (const button of this.#toolbar.querySelectorAll<HTMLButtonElement>("[data-preset]")) {
      button.setAttribute("aria-pressed", String(button.dataset.preset === this.#layout));
    }
  }

  #requireFixture(): ProofFixtureState {
    if (this.#fixture === null) throw new Error("Workspace is not initialized");
    return this.#fixture;
  }

  #requireBenchmark(): ProofBenchmarkManifest {
    if (this.#benchmark === null) throw new Error("Workspace is not initialized");
    return this.#benchmark;
  }

  #requirePane(terminalId: string): ProofTerminalPane {
    const pane = this.#panes.find((candidate) => candidate.id === terminalId);
    if (pane === undefined) throw new Error(`Unknown active terminal '${terminalId}'`);
    return pane;
  }
}
