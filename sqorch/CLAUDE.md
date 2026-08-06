# Source Context for Claude

Read the root authority and active packet first. This package uses only Python's standard library.
Keep CLI rendering separate from application behavior and do not add client, terminal, network,
packaging, or speculative abstraction code.

## File map

| Path | Purpose |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | Source context pair |
| `__init__.py` | Package marker |
| `__main__.py` | Module entry point |
| `cli.py` | Arguments and output envelopes |
| `application.py` | Doctor environment inspection |
| `authority.py` | Authority validation and manifest compilation |
| `projects.py` | Project preview and repository audit |
| `practices.py` | Practice-record lifecycle validation |
