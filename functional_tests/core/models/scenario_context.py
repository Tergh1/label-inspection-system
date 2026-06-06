from __future__ import annotations

from dataclasses import dataclass, field

from core.models.test_user import TestUser


@dataclass
class ScenarioContext:
    user: TestUser | None = None
    template_name: str | None = None
    image_name: str | None = None
    downloaded_files: dict[str, str] = field(default_factory=dict)
