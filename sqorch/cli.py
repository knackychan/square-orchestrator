import argparse
import json
import sys

from .application import ApplicationError, doctor, validate


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
        data = doctor(options.state_db) if options.command == "doctor" else validate(options.project, options.task)
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
        for label, key in (
            ("Python", "python"),
            ("Git", "git"),
            ("Repository", "repository"),
            ("State DB", "state_db"),
        ) if options.command == "doctor" else ():
            print(f"{label}: {data[key]}")
        if options.command == "validate":
            print(json.dumps(data, sort_keys=True, separators=(",", ":")))
    return 0
