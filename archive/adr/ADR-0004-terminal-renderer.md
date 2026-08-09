# ADR-0004 — Terminal renderer: reject production promotion at G0

- Status: `REJECTED_FOR_PROMOTION`
- Date: 2026-08-07
- Task: `SP00-T05`
- Candidate retained: xterm.js behind the internal terminal-view abstraction
- Revisit condition: reviewed SP00-T04 xterm.js runtime and benchmark evidence

## Context

The UI design calls for xterm.js through an internal abstraction. SP00-T04 implements a shared binding
with bounded sequence-preserving presentation queues, visible animation-frame batching, hidden-pane
throttling, catch-up, controller gating, Unicode/ANSI fixture output, resize/input messages, and
screen-reader labels.

The source and deterministic queue tests are useful evidence, but the actual xterm.js package was not
restored or executed in a Windows WebView2 or VS Code webview during the reviewed run.

## Decision

Do **not** accept xterm.js as the production terminal renderer at G0 yet. Retain it as the preferred
candidate behind the abstraction so the leaf can be replaced without changing terminal stream,
controller lease, or workflow contracts.

The renderer contract itself is retained:

- consume daemon-sequenced frames without creating workflow authority;
- make duplicates idempotent and gaps/truncation explicit;
- never silently discard bytes needed by the accepted presentation contract;
- batch visible work and throttle hidden panes without losing sequence correctness;
- accept input only while the client owns the controller lease;
- treat escape sequences as untrusted terminal data, not host commands; and
- expose accessible names, focus behavior, contrast, and screen-reader options.

## Why promotion is rejected

There is no empirical evidence for ANSI/Unicode rendering, write callbacks, resize/input behavior,
focus, screen-reader mode, high contrast, 100–200% scaling, browser memory, hidden-pane catch-up, or
high-volume responsiveness in either target host.

## Consequences

- SP05-T05 may use recorded contracts and fixtures but must not treat the renderer leaf as accepted.
- No xterm.js-specific state may enter public domain/RPC contracts.
- A renderer replacement remains permitted if the empirical proof fails.
- A superseding ADR is required before production promotion.

## Evidence required to reconsider

The SP00-T04 cross-host proof must pass every canonical fixture state and benchmark cell using the
pinned xterm.js versions, with zero sequence loss, explicit handling of any truncation, correct input
lease behavior, and reviewed accessibility/performance records from both hosts.
