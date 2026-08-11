# SA00-T03 Ordered Tasks — License, attribution, identity, telemetry, updater, and data isolation design

1. Read the authority checkout's root and nearest context pairs, `SPEC.md`, `STATUS.md`,
   `HANDOVER.md`, `CLIENT-EXECUTION.md`, the SA00-T03 packet, build guide, and target source
   context. Confirm the target is clean at `d61ec332` on `square/main`.
2. Complete client/model catalogue, allowance, visible-terminal, budget, and no-fallback
   preflight. Record the exact route and every STOP condition before editing.
3. Read the pinned source files and documentation named by the packet. Search for all requested
   identity, state, listener, telemetry, updater, release, and attribution terms.
4. Inventory LICENSE, NOTICE files, shipped third-party notices, dependency attribution, likely
   modified files, and source/release/About/installer/SBOM notice obligations. Mark unclear items
   as owner or legal review decisions.
5. Inventory runtime and release surfaces with source file or symbol, observed default, path or
   endpoint, data category, user control, and proposed Square disposition.
6. Build the official AO versus Square development versus Square release isolation matrix. Cover
   data, Electron userData, run file, SQLite, worktrees, logs/cache/crash dumps, process identity,
   listener and port, protocol, updater feed, telemetry project/key, and installer/app ID.
7. Write the ADR and three upstream inventories. Use explicit owner placeholders where evidence
   cannot establish a safe final value. Add timestamped evidence and a completion receipt.
8. Validate JSON, manifest hashes, credential-pattern counts, source preservation, diff hygiene,
   exact allowed paths, receipt completeness, and no-push status.
9. Stage only the allowlisted paths and commit exactly:

```text
docs: design SA00-T03 product identity isolation
```

10. Recheck the committed diff and clean target worktree. Report whether SA00-T04 may import or
    activate the architecture amendment. Do not claim SA00-T03 owner acceptance.
