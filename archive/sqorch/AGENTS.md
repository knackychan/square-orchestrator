# Source Context

This directory contains the standard-library Python CLI. Keep argument parsing and rendering in
`cli.py`, orchestration in `application.py`, and never launch clients, terminals, or network calls
from the M1 dry-run foundation.

## File map

| Path | Purpose |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | Source context pair |
| `__init__.py` | Package marker |
| `__main__.py` | `python -m sqorch` entry point |
| `cli.py` | Arguments and human/JSON rendering |
| `application.py` | Doctor use case and environment inspection |
| `authority.py` | Fail-closed task authority validation and canonical manifest compilation |
| `projects.py` | Responsibility-graph preview and read-only repository audit |
| `practices.py` | Practice-record JSON validation against closed lifecycle vocabulary |
| `state.py` | SQLite project registry and holder-bound write locks |
