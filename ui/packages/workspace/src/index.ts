export const LAYOUT_SCHEMA_VERSION = 1 as const;
export type PaneType = "agentFleet" | "terminal" | "approvals" | "events" | "taskGraph" | "plan" | "review" | "resources" | "inspector" | "compatibility";
export interface Pane { readonly id: string; readonly type: PaneType; readonly title: string; readonly sourceType?: string; }
export interface Layout { readonly schemaVersion: 1; readonly preset: "Operations" | "Focus Agent" | "Plan" | "Review" | "Resources"; readonly panes: readonly Pane[]; }
const pane = (id: string, type: PaneType, title: string): Pane => Object.freeze({ id, type, title });
export const layoutPresets: Readonly<Record<Layout["preset"], Layout>> = Object.freeze({
  Operations: Object.freeze({ schemaVersion: 1, preset: "Operations", panes: Object.freeze([pane("fleet","agentFleet","Agent Fleet"), pane("terminal","terminal","Selected Terminal"), pane("approvals","approvals","Approvals"), pane("events","events","Events"), pane("inspector","inspector","Inspector")]) }),
  "Focus Agent": Object.freeze({ schemaVersion: 1, preset: "Focus Agent", panes: Object.freeze([pane("terminal","terminal","Focused Terminal"), pane("inspector","inspector","Inspector")]) }),
  Plan: Object.freeze({ schemaVersion: 1, preset: "Plan", panes: Object.freeze([pane("graph","taskGraph","Task Graph"), pane("plan","plan","Plan and Acceptance"), pane("events","events","Context and Evidence"), pane("inspector","inspector","Inspector")]) }),
  Review: Object.freeze({ schemaVersion: 1, preset: "Review", panes: Object.freeze([pane("review","review","Diff and Review"), pane("terminal","terminal","Review Terminal"), pane("plan","plan","Acceptance Criteria"), pane("inspector","inspector","Inspector")]) }),
  Resources: Object.freeze({ schemaVersion: 1, preset: "Resources", panes: Object.freeze([pane("fleet","agentFleet","Agent Fleet"), pane("resources","resources","Route and Resource Health"), pane("events","events","Resource Events"), pane("inspector","inspector","Inspector")]) })
});
const knownPaneTypes = new Set(["agentFleet","terminal","approvals","events","taskGraph","plan","review","resources","inspector","compatibility"]);
export function restoreLayout(value: unknown): Layout {
  if (!isRecord(value) || value.schemaVersion !== 1 || typeof value.preset !== "string" || !Array.isArray(value.panes)) return layoutPresets.Operations;
  const seen = new Set<string>(); const panes: Pane[] = [];
  for (const item of value.panes) {
    if (!isRecord(item) || typeof item.id !== "string" || item.id.length === 0 || typeof item.type !== "string" || typeof item.title !== "string" || seen.has(item.id)) return layoutPresets.Operations;
    seen.add(item.id);
    panes.push(knownPaneTypes.has(item.type) ? Object.freeze({ id: item.id, type: item.type as PaneType, title: item.title }) : Object.freeze({ id: item.id, type: "compatibility", title: `Unavailable pane: ${item.title}`, sourceType: item.type }));
  }
  if (!(value.preset in layoutPresets)) return layoutPresets.Operations;
  return Object.freeze({ schemaVersion: 1, preset: value.preset as Layout["preset"], panes: Object.freeze(panes) });
}
export interface EntityState<T> { readonly sequence: number; readonly entities: Readonly<Record<string, T>>; readonly gap: { readonly expected: number; readonly received: number } | null; }
export function createEntityState<T>(): EntityState<T> { return Object.freeze({ sequence: 0, entities: Object.freeze({}), gap: null }); }
export function applyEntityDelta<T>(state: EntityState<T>, sequence: number, upserts: Readonly<Record<string, T>>, removals: readonly string[] = []): EntityState<T> {
  if (!Number.isSafeInteger(sequence) || sequence <= 0) throw new RangeError("sequence must be a positive integer");
  if (sequence <= state.sequence) return state; const expected = state.sequence + 1;
  if (sequence !== expected) return Object.freeze({ ...state, gap: { expected, received: sequence } });
  const entities: Record<string, T> = { ...state.entities, ...upserts }; for (const id of removals) delete entities[id];
  return Object.freeze({ sequence, entities: Object.freeze(entities), gap: null });
}
function isRecord(value: unknown): value is Record<string, unknown> { return typeof value === "object" && value !== null && !Array.isArray(value); }
