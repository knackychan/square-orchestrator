# Test Context for Claude

Read the root authority and active packet first. Tests are standard-library-only, own their
temporary state, and make no network, provider, terminal, or agent-client calls.

## File map

| Path | Purpose |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | Test context pair |
| `support.py` | Shared test fixtures |
| `test_cli.py` | CLI argument and output-contract tests |
| `test_authority.py` | Authority manifest tests |
