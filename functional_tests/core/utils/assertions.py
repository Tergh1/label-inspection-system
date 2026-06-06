from __future__ import annotations

from playwright.sync_api import Locator, expect


def expect_visible(locator: Locator, timeout: int | None = None) -> None:
    expect(locator).to_be_visible(timeout=timeout)


def expect_contains_text(locator: Locator, text: str, timeout: int | None = None) -> None:
    expect(locator).to_contain_text(text, timeout=timeout)
