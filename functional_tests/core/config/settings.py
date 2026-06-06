from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path

from dotenv import load_dotenv


ROOT_DIR = Path(__file__).resolve().parents[3]
PROJECT_ROOT = ROOT_DIR.parent


@dataclass(frozen=True)
class Settings:
    base_url: str = "http://localhost:7108"
    headless: bool = True
    slow_mo_ms: int = 0
    default_timeout_ms: int = 10_000
    ml_timeout_ms: int = 10_000
    test_user_domain: str = "example.test"


def _read_bool(name: str, default: bool) -> bool:
    value = os.getenv(name)
    if value is None:
        return default

    return value.strip().lower() in {"1", "true", "yes", "on"}


def load_settings() -> Settings:
    load_dotenv(ROOT_DIR / ".env")

    return Settings(
        base_url=os.getenv("BASE_URL", Settings.base_url).rstrip("/"),
        headless=_read_bool("HEADLESS", Settings.headless),
        slow_mo_ms=int(os.getenv("SLOW_MO_MS", Settings.slow_mo_ms)),
        default_timeout_ms=int(os.getenv("DEFAULT_TIMEOUT_MS", Settings.default_timeout_ms)),
        ml_timeout_ms=int(os.getenv("ML_TIMEOUT_MS", Settings.ml_timeout_ms)),
        test_user_domain=os.getenv("TEST_USER_DOMAIN", Settings.test_user_domain),
    )
