from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class TemplateUploadData:
    friendly_name: str
    file_path: str
    tolerance_percent: str = "20"
    description: str = "BDD reference template"


@dataclass(frozen=True)
class ImageUploadData:
    file_path: str
    tolerance_percent: str = "20"
    description: str = "BDD inspected image"
