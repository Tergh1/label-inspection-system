from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class TestUser:
    email: str
    password: str
