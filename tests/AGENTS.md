# Test Context

Tests use only Python's standard library and temporary directories. They must not depend on
installed agent clients, network access, user state databases, or another repository.

## File map

| Path | Purpose |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | Test context pair |
| `test_cli.py` | CLI argument and output-contract tests |
