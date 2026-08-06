# Test Context

Tests use only Python's standard library and temporary directories. They must not depend on
installed agent clients, network access, user state databases, or another repository.

## File map

| Path | Purpose |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | Test context pair |
| `support.py` | Shared test fixtures |
| `test_cli.py` | CLI argument and output-contract tests |
| `test_authority.py` | Authority manifest tests |
| `test_projects.py` | Project preview and audit tests |
| `test_practices.py` | Practice record validation tests |
