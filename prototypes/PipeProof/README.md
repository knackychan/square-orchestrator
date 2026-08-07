# SP00-T03 — PipeProof

PipeProof is the isolated architecture prototype for Square Orchestrator's local .NET/Node named-pipe
transport. It is not referenced by production projects and it does not promote protocol code into the
daemon. Promotion remains blocked until the Windows evidence is executed, reviewed, and accepted at G0.

## Implemented proof boundary

The prototype implements:

- four-byte unsigned big-endian framing of strict UTF-8 JSON, with a one MiB canonical payload limit;
- a mandatory first-frame `hello` handshake and typed incompatible-protocol/version errors;
- versioned request/response, cancellation, subscribe/unsubscribe, event, subscription-close, and
  server-going-away messages;
- exact unknown-field rejection in both .NET and Node;
- a .NET server core separated from the Windows named-pipe transport;
- .NET and Node clients using the same fixtures and protocol vectors;
- a protected Windows DACL granting the current user SID and LocalSystem only;
- local-only pipes through `PIPE_REJECT_REMOTE_CLIENTS`;
- an anonymous-token negative access probe that must fail with `ERROR_ACCESS_DENIED` before the server
  writes readiness;
- bounded control, event-presentation, local-client, and per-subscription queues;
- durable monotonic event sequence numbers in an append-only NDJSON journal;
- replay after an exclusive cursor, typed refusal for stale/ahead/oversized replay windows, and
  reconnect after deliberate daemon termination;
- transactional subscription activation: acknowledgement, replay queueing, and live registration are
  ordered under the publish gate so a publisher cannot overtake the acknowledgement or replay window;
- fail-closed evidence identity for the canonical dispatch packet and scenario manifest; and
- raw environment, server-generation, process, scenario, summary, and SHA-256 evidence files.

## Project layout

```text
Square.PipeProof.Protocol/            framing, messages, strict validation, shared vectors
Square.PipeProof.Client/              host-neutral .NET client and reconnecting subscription
Square.PipeProof.ServerCore/          transport-neutral server, journal, subscriptions, queues
Square.PipeProof.Transport.Windows/   CreateNamedPipeW, live ACL inspection, negative probe
Square.PipeProof.Server/              Windows server executable and readiness/final evidence
Square.PipeProof.DotNetClient/        .NET contract-parity fixture executable
Square.PipeProof.Harness/             Windows scenario runner and evidence writer
Square.PipeProof.Tests/               dependency-free deterministic .NET contract tests
node-client/                          Node codec, protocol client, parity and reconnect fixtures
fixtures/contracts/                   thirteen golden message fixtures
```

`PipeProof.slnx` contains all eight .NET projects. None of them references `src/`, and no production
project references this prototype.

## Wire rules

Each frame is:

```text
[4-byte unsigned big-endian payload length][UTF-8 JSON payload]
```

A length of zero, a length above the declared maximum, truncated content, malformed JSON, invalid
UTF-8, unknown fields, unknown message kinds, or invalid message invariants is rejected. Allocation
occurs only after the prefix has passed the configured size bound.

The first message must be `hello`. A compatible server returns `hello_ack` with its instance/epoch,
capabilities, queue limits, and current journal bounds. Incompatible clients receive a typed
`protocol_error` and the connection closes.

For subscriptions, `from_sequence` is an exclusive cursor. A value of zero means "start live at the
server's current cursor" rather than "replay the entire journal." The acknowledgement reports
`live_from_sequence`, `replayed_through_sequence`, and the retained range. The reconnecting clients
advance their cursor to the acknowledged live boundary even when no event has yet arrived, preventing
a disconnect-before-first-event gap.

## Backpressure model

The proof never uses an unbounded transport or subscription queue.

- Control frames and event-presentation frames use separate bounded channels.
- Control frames are selected first and are never replaced by event traffic.
- A full event-presentation queue records a drop instead of growing memory.
- A full per-subscription queue closes that subscription with a typed resume cursor.
- A write that exceeds its deadline cancels the connection.
- Durable journal writes occur before an event becomes visible to subscribers.

Presentation-frame loss is therefore observable and recoverable through a replay cursor, while the
journal remains authoritative for this proof.

## Canonical Windows scenarios

The full harness executes these ordered scenarios:

1. `acl-security`
2. `cross-language-parity`
3. `version-negotiation`
4. `framing-failures`
5. `disconnect-replay`
6. `daemon-restart-reconnect`
7. `slow-subscriber`
8. `replay-window-refusal`
9. `graceful-shutdown`

Quick mode executes the first four only and can produce `DIAGNOSTIC_PASS`, never acceptance `PASS`.
A full `PASS` additionally requires normal-user Windows x64, .NET SDK 10.0.302, Node v24.19.0, the
canonical source hashes, every scenario, and no scenario failure.

## Running the proof

From a normal, non-elevated Windows x64 checkout with the pinned toolchains:

```powershell
./prototypes/PipeProof/run-proof.ps1
```

A reduced diagnostic run is:

```powershell
./prototypes/PipeProof/run-proof.ps1 -Quick
```

The script validates source isolation, runs Node and .NET deterministic tests, builds the isolated
solution, and invokes the Windows harness. Evidence is written under
`artifacts/proofs/SP00-T03/<UTC timestamp>/` unless `-EvidenceDirectory` is supplied.

## Evidence files

A normal run produces at least:

```text
environment.json
inputs/dispatch.packet.json
inputs/scenario-manifest.json
servers/generation-*/ready.json
servers/generation-*/final.json
servers/generation-*/process.json
servers/generation-*/stdout.log
servers/generation-*/stderr.log
scenarios.ndjson
summary.json
evidence-manifest.sha256
```

`summary.json` conforms to `evidence.schema.json`. `PASS` is impossible when quick mode is used, a
pinned toolchain differs, the process is elevated, architecture is not x64, canonical input hashes
differ, a scenario is missing, or any scenario fails.

## Current validation status

The host-neutral source validator and Node contract suite can run on non-Windows systems. Actual named
pipe creation, live DACL inspection, anonymous-token denial, .NET compilation, cross-language pipe
traffic, restart replay, slow-subscriber measurements, and graceful shutdown require Windows and are
not represented as executed merely because the source exists.
