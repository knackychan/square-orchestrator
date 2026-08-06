from pathlib import Path


_VALID_STATES = frozenset({"OBSERVED", "CANDIDATE", "TRIAL", "ADOPTED", "REJECTED", "DEPRECATED"})

_REQUIRED_FIELDS = frozenset(
    {
        "id",
        "category",
        "statement",
        "scope",
        "source_type",
        "provenance",
        "context",
        "outcome",
        "trade_offs",
        "counterexamples",
        "confidence",
        "review_date",
        "state",
        "approving_authority",
        "affected_versions",
        "opted_in_projects",
    }
)

_STRING_FIELDS = frozenset(
    {"id", "category", "statement", "scope", "source_type", "context", "outcome", "review_date"}
)

_LIST_FIELDS = frozenset({"trade_offs", "counterexamples", "affected_versions", "opted_in_projects"})


class PracticeError:
    def __init__(self, code: str, message: str) -> None:
        self.code = code
        self.message = message


class _ValidationResult:
    def __init__(self, record: dict[str, object] | None = None, error: PracticeError | None = None) -> None:
        self._record = record
        self.error = error

    def __getitem__(self, key: str) -> object:
        if self._record is None:
            raise KeyError(key)
        return self._record[key]


def validate(record: dict[str, object]) -> _ValidationResult:
    key_set = set(record)
    if key_set != _REQUIRED_FIELDS:
        return _ValidationResult(error=PracticeError("INVALID_INPUT", "Practice record has incomplete or unknown fields"))

    for field in _STRING_FIELDS:
        value = record[field]
        if not isinstance(value, str) or not value:
            return _ValidationResult(error=PracticeError("INVALID_INPUT", f"Practice record has invalid {field}"))

    for field in _LIST_FIELDS:
        value = record[field]
        if not isinstance(value, list) or not all(isinstance(item, str) for item in value):
            return _ValidationResult(error=PracticeError("INVALID_INPUT", f"Practice record has invalid {field}"))

    provenance = record["provenance"]
    if not isinstance(provenance, str) or not provenance:
        return _ValidationResult(error=PracticeError("INVALID_INPUT", "Practice record must have a non-empty provenance"))

    approving_authority = record["approving_authority"]
    if approving_authority is not None and (
        not isinstance(approving_authority, str) or not approving_authority
    ):
        return _ValidationResult(error=PracticeError("INVALID_INPUT", "Practice record has invalid approving_authority"))

    confidence = record["confidence"]
    if not isinstance(confidence, (int, float)) or isinstance(confidence, bool):
        return _ValidationResult(error=PracticeError("INVALID_INPUT", "Practice record has invalid confidence"))
    if not (0 <= confidence <= 1):
        return _ValidationResult(error=PracticeError("INVALID_INPUT", "Practice confidence must be between 0 and 1"))

    state = record["state"]
    if not isinstance(state, str) or state not in _VALID_STATES:
        return _ValidationResult(error=PracticeError("INVALID_INPUT", f"Practice state must be one of {sorted(_VALID_STATES)}"))

    if state == "ADOPTED" and (approving_authority is None or not isinstance(approving_authority, str) or not approving_authority):
        return _ValidationResult(error=PracticeError("INVALID_INPUT", "ADOPTED practice must have an approving_authority"))

    return _ValidationResult(record=record)
