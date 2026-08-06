import argparse
import json
import sys

from .application import ApplicationError, audit_projects, doctor, preview_projects, project_add, run_dry_run, validate, validate_practices


class CommandError(Exception):
    pass


class ArgumentParser(argparse.ArgumentParser):
    def error(self, message: str) -> None:
        raise CommandError(message)


def _parser() -> ArgumentParser:
    parser = ArgumentParser(prog="sqorch")
    parser.add_argument("--json", action="store_true", dest="json_output")
    parser.add_argument("--state-db")
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("doctor")
    validate_command = commands.add_parser("validate")
    validate_command.add_argument("--project", required=True)
    validate_command.add_argument("--task", required=True)
    project_command = commands.add_parser("project")
    project_subcommands = project_command.add_subparsers(dest="project_command", required=True)
    new_command = project_subcommands.add_parser("new")
    new_command.add_argument("--input", required=True)
    new_command.add_argument("--preview", action="store_true")
    adopt_command = project_subcommands.add_parser("adopt")
    adopt_command.add_argument("path")
    adopt_command.add_argument("--audit-only", action="store_true")
    add_command = project_subcommands.add_parser("add")
    add_command.add_argument("path")
    practices_command = commands.add_parser("practices")
    practices_subcommands = practices_command.add_subparsers(dest="practices_command", required=True)
    validate_practices_command = practices_subcommands.add_parser("validate")
    validate_practices_command.add_argument("path")
    run_command = commands.add_parser("run")
    run_command.add_argument("--project", required=True)
    run_command.add_argument("--task", required=True)
    run_command.add_argument("--dry-run", action="store_true")
    return parser


def _write_json(value: object) -> None:
    print(json.dumps(value, sort_keys=True, separators=(",", ":")))


def main(argv: list[str] | None = None) -> int:
    arguments = sys.argv[1:] if argv is None else argv
    json_requested = "--json" in arguments
    try:
        options = _parser().parse_args(arguments)
    except CommandError as error:
        if json_requested:
            _write_json(
                {
                    "ok": False,
                    "error": {"code": "INVALID_INPUT", "message": str(error), "details": {}},
                }
            )
        else:
            print(f"error: {error}", file=sys.stderr)
        return 2

    try:
        if options.command == "doctor":
            data = doctor(options.state_db)
        elif options.command == "validate":
            data = validate(options.project, options.task)
        elif options.command == "project":
            if options.project_command == "new":
                if not options.preview:
                    raise ApplicationError(
                        "INVALID_INPUT",
                        "project new requires --preview.",
                        exit_code=2,
                    )
                data = preview_projects(options.input)
            elif options.project_command == "add":
                data = project_add(options.path, state_db=options.state_db)
            else:
                if not options.audit_only:
                    raise ApplicationError(
                        "INVALID_INPUT",
                        "project adopt requires --audit-only.",
                        exit_code=2,
                    )
                data = audit_projects(options.path)
        elif options.command == "practices":
            data = validate_practices(options.path)
        elif options.command == "run":
            if not options.dry_run:
                raise ApplicationError(
                    "INVALID_INPUT",
                    "run requires --dry-run in M1.",
                    exit_code=2,
                )
            data = run_dry_run(options.project, options.task, state_db=options.state_db)
        else:
            raise ApplicationError("INVALID_INPUT", f"Unknown command: {options.command}", exit_code=2)
    except ApplicationError as error:
        result = {
            "ok": False,
            "error": {"code": error.code, "message": error.message, "details": {}},
        }
        if options.json_output:
            _write_json(result)
        else:
            print(f"{error.code}: {error.message}", file=sys.stderr)
        return error.exit_code

    if options.json_output:
        _write_json({"ok": True, "data": data})
    else:
        if options.command == "doctor":
            for label, key in (
                ("Python", "python"),
                ("Git", "git"),
                ("Repository", "repository"),
                ("State DB", "state_db"),
            ):
                print(f"{label}: {data[key]}")
        elif options.command == "validate":
            print(json.dumps(data, sort_keys=True, separators=(",", ":")))
        elif options.command == "practices":
            print(json.dumps(data, sort_keys=True, separators=(",", ":")))
        elif options.command == "project":
            if options.project_command == "new":
                print(f"Dependency order: {' -> '.join(data['dependency_order'])}")
                print("Authority files:")
                for name in data["authority_files"]:
                    print(f"  {name}")
                print("Context pairs:")
                for name in data["context_pairs"]:
                    print(f"  {name}")
            elif options.project_command == "add":
                print(f"Registered: {data['project_path']}")
                print(f"Profile: {data['profile_path']}")
            else:
                print(f"HEAD: {data['head']}")
                print(f"Worktree clean: {data['worktree_clean']}")
                print(f"Active packet exists: {data['active_packet_exists']}")
        elif options.command == "run":
            print(f"Task: {data['task_id']}")
            print(f"Project: {data['project_path']}")
            print(f"Route: {data['route']['client']} / {data['route']['model']}")
            print(f"Launch performed: {data['launch_performed']}")
            print(f"Automatic fallback: {data['automatic_fallback']}")
    return 0
