from enum import StrEnum


class OutcomeStatus(StrEnum):
    PENDING = "Pending"
    VALID = "Valid"
    VALID_WITH_DEFECTS = "ValidWithDefects"
    INVALID = "Invalid"
