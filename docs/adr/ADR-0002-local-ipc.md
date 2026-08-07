# ADR-0002 — Local IPC: reject production promotion at G0

- Status: `REJECTED_FOR_PROMOTION`
- Date: 2026-08-07
- Task: `SP00-T05`
- Candidate retained: ACL-restricted local Windows named pipe with length-framed strict UTF-8 JSON
- Revisit condition: reviewed acceptance-eligible SP00-T03 Windows evidence

## Context

SP00-T03 implements a .NET server/client and Node client around a four-byte big-endian frame length,
strict JSON envelopes, handshake/versioning, request/response, cancellation, subscriptions, durable
sequence replay, reconnect, bounded queues, protected DACL inspection, and an anonymous negative-access
probe. The Node extension host—not a webview—owns the connection.

The implementation has host-neutral contract evidence, but no live Windows named-pipe, ACL, restart,
replay, or slow-subscriber result was produced in the creation environment.

## Evidence reviewed

| Evidence | SHA-256 | Result available |
|---|---|---|
| `docs/proofs/named-pipe-protocol.md` | `d03b04bd19e0fa84d07d4deb1601843c68ddd9aae0653d68fb9d7a97170ba80c` | Source/proof contract only |
| `docs/receipts/SP00-T03.prototype-receipt.json` | `85c3c0cdd86c5b854add20a9b0f9659c8852a2972a962af8051920dc7489bf37` | Windows execution pending |
| `docs/validation/sp00-t03-host-neutral-validation.txt` | `be7c0c431940b09871a2ee1058d45007736820a1fb0c868489d79ccbb842d32d` | Host-neutral PASS |
| `prototypes/PipeProof/dispatch.packet.json` | `aa959c46c51d669f9677c0155876a2ff102316c856150ac6904cbdf01e2d171d` | Canonical input |
| `prototypes/PipeProof/scenario-manifest.json` | `5479ad46d8502d6ebe365de3386e558e4029bea03e724d2f19aa2c3102d1b355` | 9 scenarios |
| Acceptance-eligible Windows evidence | not present | **Missing** |

## Decision

Do **not** promote PipeProof transport code into `Square.Daemon` or `Square.Platform.Windows` at G0.
Keep the named-pipe/framed-JSON design as the current candidate and keep the protocol/domain boundary
technology-neutral.

The following protocol invariants remain required if this or another leaf is selected:

- a mandatory version handshake before dispatch;
- bounded frames, requests, subscribers, replay, and presentation queues;
- typed incompatible-version, malformed-message, cancellation, replay, and backpressure results;
- monotonic event sequence and explicit replay/truncation semantics;
- current-user local access rather than a default broad pipe ACL;
- no pipe handle or arbitrary filesystem/process authority in webviews; and
- daemon-only authoritative state mutation.

## Why promotion is rejected

No reviewed evidence proves live `CreateNamedPipeW` behavior, the protected DACL, anonymous denial,
fragmented/coalesced stream behavior, .NET/Node parity on the real transport, forced daemon restart,
reconnect/replay, bounded slow-subscriber behavior, or graceful shutdown under the pinned versions.

## Consequences

- SP02-T04 remains blocked.
- The 1 MiB frame limit and prototype queue sizes remain proof constants, not production budgets.
- No client may be given direct SQLite access as a workaround.
- A later accepted or rejected leaf decision must supersede this ADR explicitly.

## Evidence required to reconsider

A normal, non-elevated Windows x64 run of:

```powershell
./prototypes/PipeProof/run-proof.ps1
```

must execute all nine canonical scenarios with the pinned .NET and Node versions, preserve the declared
bounds, pass live DACL and negative-access checks, prove restart/reconnect/replay, and produce a complete
SHA-256 evidence manifest with final `PASS` status.
