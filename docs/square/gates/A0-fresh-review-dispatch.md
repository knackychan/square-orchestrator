# Fresh independent A0 review dispatch

Review candidate tip `c9458bd201f577e3aacb092a5e3ae3a3e5d3faa2` on branch
`square/sa00-fix02-03` in an isolated read-only worktree.

This is a fresh superseding `SA00-T05 — Adoption gate A0` review. Do not use
the prior blocked receipt as the review result; use it as the blocker history.

Read:

- `docs/square/gates/A0-adoption-review.md`
- `docs/square/gates/A0-adoption-review.json`
- `docs/square/receipts/SA00-T05.json`
- `docs/square/receipts/SA00-FIX02.json`
- `docs/square/receipts/SA00-FIX03.json`
- `docs/square/evidence/SA00-FIX02/20260811T051919Z/`
- `docs/square/evidence/SA00-FIX03/20260811T051919Z/`
- `docs/square/authority/00-architecture-amendment.md`
- `docs/square/authority/01-master-session-first-plan.md`

Independently verify every A0-01 through A0-12 criterion against committed Git
bytes. Verify both fix manifests, the canonical `upstream` remote, clean-tree
classification, Square/AO state/process/port/environment/app isolation,
telemetry hard-off, updater hard-off through SA14, and Apache-2.0 authority.

Required result:

- write a new superseding review under `docs/square/evidence/SA00-T05/<UTC>/`;
- write a new `docs/square/receipts/SA00-T05.json` only in the review branch;
- preserve prior receipts and evidence;
- mark A0 `PASS` only if all twelve criteria independently pass;
- otherwise mark `BLOCKED` with surgical `SA00-FIXxx` scope;
- do not dispatch SA01 or any later milestone from this review.
