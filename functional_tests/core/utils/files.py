from __future__ import annotations

import re
from pathlib import Path

from core.config.settings import ROOT_DIR


def sample_file(name: str) -> str:
    path = ROOT_DIR / "models" / "Pharmacy" / name
    if not path.exists():
        raise FileNotFoundError(f"Sample file not found: {path}")
    return str(path)


def slugify(value: str) -> str:
    slug = re.sub(r"[^a-zA-Z0-9]+", "-", value).strip("-").lower()
    return slug[:50] or "scenario"
