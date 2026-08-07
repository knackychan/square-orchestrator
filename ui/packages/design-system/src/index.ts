export const semanticTokens = Object.freeze({
  canvas: "var(--square-canvas)", panel: "var(--square-panel)", panelRaised: "var(--square-panel-raised)", divider: "var(--square-divider)",
  textPrimary: "var(--square-text-primary)", textSecondary: "var(--square-text-secondary)", textDisabled: "var(--square-text-disabled)",
  focus: "var(--square-focus)", stateInfo: "var(--square-state-info)", stateSuccess: "var(--square-state-success)", stateWarning: "var(--square-state-warning)", stateDanger: "var(--square-state-danger)"
});

export type OperationalState = "running" | "quiet_active" | "waiting_for_input" | "waiting_for_approval" | "auth_required" | "blocked" | "suspected_stall" | "succeeded" | "failed" | "telemetry_degraded";
export interface StatePresentation { readonly text: string; readonly symbol: string; readonly tone: "info" | "success" | "warning" | "danger"; }
export const statePresentations: Readonly<Record<OperationalState, StatePresentation>> = Object.freeze({
  running: { text: "Running", symbol: "▶", tone: "info" },
  quiet_active: { text: "Quiet active", symbol: "…", tone: "info" },
  waiting_for_input: { text: "Waiting for input", symbol: "?", tone: "warning" },
  waiting_for_approval: { text: "Waiting for approval", symbol: "!", tone: "warning" },
  auth_required: { text: "Authentication required", symbol: "⌁", tone: "warning" },
  blocked: { text: "Blocked", symbol: "■", tone: "danger" },
  suspected_stall: { text: "Suspected stall", symbol: "◷", tone: "warning" },
  succeeded: { text: "Succeeded", symbol: "✓", tone: "success" },
  failed: { text: "Failed", symbol: "×", tone: "danger" },
  telemetry_degraded: { text: "Telemetry degraded", symbol: "△", tone: "warning" }
});
export const density = Object.freeze({ compactRowPx: 28, comfortableRowPx: 32, minimumInteractivePx: 28 });
