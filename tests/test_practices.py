from pathlib import Path
import unittest

from tests.support import tree_digest


class PracticeValidationTests(unittest.TestCase):
    def _candidate(self, **overrides):
        record = {
            "id": "P-001",
            "category": "testing",
            "statement": "Always write tests first",
            "scope": "project",
            "source_type": "observation",
            "provenance": "T-M1-04 review",
            "context": "Test project M1 dry run",
            "outcome": "Reduced regression defects",
            "trade_offs": ["slower initial velocity"],
            "counterexamples": [],
            "confidence": 0.9,
            "review_date": "2026-08-06",
            "state": "CANDIDATE",
            "approving_authority": None,
            "affected_versions": [],
            "opted_in_projects": [],
        }
        record.update(overrides)
        return record

    def test_candidate_accepted(self):
        from sqorch.practices import validate

        result = validate(self._candidate())
        assert result["state"] == "CANDIDATE"

    def test_missing_provenance_rejected(self):
        from sqorch.practices import validate

        result = validate(self._candidate(provenance=None))
        assert result.error.code == "INVALID_INPUT"

    def test_confidence_above_one_rejected(self):
        from sqorch.practices import validate

        result = validate(self._candidate(confidence=1.5))
        assert result.error.code == "INVALID_INPUT"

    def test_adopted_without_authority_rejected(self):
        from sqorch.practices import validate

        result = validate(self._candidate(state="ADOPTED", approving_authority=None))
        assert result.error.code == "INVALID_INPUT"

    def test_unchanged_repository_digest(self):
        from sqorch.practices import validate

        repository = Path.cwd()
        unchanged_digest = tree_digest(repository)
        validate(self._candidate())
        assert tree_digest(repository) == unchanged_digest


if __name__ == "__main__":
    unittest.main()
