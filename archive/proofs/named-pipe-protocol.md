# SP00-T03 named-pipe framing and reconnect proof

## Status

**Source implementation complete; empirical Windows x64 acceptance pending.**

This document records the candidate leaf architecture implemented under `prototypes/PipeProof/`. The
prototype remains isolated from production modules. It can inform SP02-T04 only after its Windows
evidence has been executed and accepted at G0.

## Decision under test

Use one local, per-user Windows named pipe carrying length-prefixed strict UTF-8 JSON envelopes. The
Node extension host and .NET clients own the pipe connection; webviews never receive pipe handles.
Only the daemon owns authoritative orchestration state.

### Framing

```text
uint32 big-endian payload length
UTF-8 JSON payload
```

The canonical frame payload limit is 1,048,576 bytes. Zero, oversized, truncated, malformed, invalid
UTF-8, unknown-kind, unknown-field, and invariant-invalid frames are rejected before dispatch.
Fragmentation and coalescing are transport details: both decoders preserve message boundaries across
arbitrary stream chunks.

### Handshake and compatibility

The first frame is a `hello` envelope containing protocol/version and client identity. The server
returns `hello_ack` with server instance/epoch, limits, capabilities, and journal bounds. Protocol or
version mismatches receive a typed `protocol_error` plus supported versions; no application message is
dispatched before compatibility succeeds.

### Request lifecycle

Requests use unique IDs and receive exactly one result or typed error. Explicit `cancel` messages target
active request IDs. Duplicate IDs, unknown methods, invalid parameters, in-flight limits, cancellation,
and internal errors are typed. The proof includes a cancellable delayed request and confirms the
cancellation acknowledgement and final cancelled response in both .NET and Node fixtures.

### Subscription and replay lifecycle

Events receive durable, monotonically increasing global sequence numbers. A subscriber supplies an
exclusive `from_sequence` cursor. Zero means live-only from the current server boundary; a positive
cursor requests retained events after that value.

Subscription activation is transactional under the event publish gate:

1. validate the retained range and replay budget;
2. construct the bounded replay queue;
3. add the connection-local subscription;
4. enqueue `subscribed` on the bounded control queue;
5. start replay delivery; and
6. release the publish gate so live publication can resume.

This prevents a live publisher from overtaking the acknowledgement or the replay set. Reconnecting
clients advance their cursor to the acknowledged live boundary before waiting for the first event.

A cursor older than retained history, ahead of the journal, or requiring more than the configured
replay budget is refused with a typed error and current bounds. The client must request an explicit
snapshot or choose a newer cursor rather than receiving an unbounded history dump.

### Durable restart behavior

The proof journal is append-only NDJSON opened with write-through intent. Each line contains sequence,
topic, type, payload, timestamp, and event ID. The server validates contiguous history on startup,
retains a bounded in-memory replay window, and increments a durable server epoch at every start.

The restart scenario deliberately terminates the daemon process, restarts it against the same state
directory, verifies a changed instance/epoch, and requires Node reconnect plus sequence replay from the
last consumed cursor.

## Security boundary

The Windows transport calls `CreateNamedPipeW` with:

- `PIPE_REJECT_REMOTE_CLIENTS`;
- a protected explicit DACL;
- one full-control ACE for LocalSystem (`S-1-5-18`); and
- one full-control ACE for the current user SID.

The created kernel object's live DACL is queried and inspected. Readiness fails if the DACL is not
protected, contains inherited/broader access, lacks either expected SID, or contains anything other
than the two expected full-control allow ACEs.

Before readiness, the server impersonates an anonymous thread token and attempts to open the pipe. The
probe must fail with `ERROR_ACCESS_DENIED`; reverting the thread token is fail-fast. This provides a
deterministic negative principal test without requiring CI credentials for a second account. An actual
alternate-account test can be added by CI, but it is not substituted for live DACL inspection.

## Bounded memory and slow subscribers

There are four bounded queue boundaries:

| Boundary | Canonical proof capacity | Failure behavior |
|---|---:|---|
| connection control frames | 8 | bounded wait; connection closes on deadline/failure |
| connection event-presentation frames | 8 | event frame is not enqueued; metric increments |
| server subscription queue | 8 | subscription closes with typed resume cursor |
| local client subscription queue | 256 | client connection fails and reconnect logic resumes |

The event journal retention is 64 events and one subscribe may replay at most 8. The slow-subscriber
scenario publishes large events while the subscriber does not read, requires observed bounded depths,
requires a backpressure disconnect/drop signal, and confirms a separate control client remains usable.
No queue uses unbounded mode.

## Contract parity

Thirteen golden envelopes and one shared vector document are consumed by .NET and Node tests. The
vector document contains valid canonical messages and invalid cases. The live parity scenario executes
Unicode echo, cancellation, publication, subscription, sequence checks, and declared queue-bound checks
through both clients, then compares normalized outcomes while excluding the client label.

## Canonical scenario set

1. ACL structure, current-user positive access, and anonymous negative access.
2. .NET/Node contract parity.
3. Incompatible-version rejection.
4. Fragmented/coalesced traffic plus oversized, malformed, invalid UTF-8, and truncated frames.
5. Disconnect and replay from an exclusive cursor.
6. Daemon crash, durable restart, Node reconnect, and replay.
7. Slow-subscriber bounded backpressure.
8. Stale and excessive replay refusal.
9. Graceful shutdown notification, drain, and zero active connections.

## Acceptance shape

An acceptance-eligible run must:

- execute all nine scenarios as a normal non-elevated Windows x64 process;
- use .NET SDK 10.0.302 and Node v24.19.0;
- use the canonical dispatch and scenario-manifest SHA-256 identities;
- pass every scenario;
- preserve the bounded queue limits and durable sequence invariants; and
- produce a complete SHA-256 evidence manifest.

Quick mode or any altered input can produce diagnostic evidence only. A technical failure always
produces `FAIL` and leaves SP00-T03 unaccepted.

## Recovery and promotion boundary

This proof validates stream reconnect and event replay; it does not grant clients direct database
access, move state authority into a UI, or define remote/network transport. Production promotion must
preserve the protocol/server-core versus Windows-transport split and keep the Node connection in the
extension host. The exact source is not automatically production-ready merely because the proof passes;
SP02-T04 must adopt the accepted contract deliberately and add production persistence/idempotency tests.

## Execution still required

The current creation environment cannot execute Windows kernel ACLs/named pipes or compile the .NET 10
projects. The acceptance command remains:

```powershell
./prototypes/PipeProof/run-proof.ps1
```

Until a complete `PASS` evidence directory is reviewed, this proof is implemented but not accepted and
G0 remains blocked.
