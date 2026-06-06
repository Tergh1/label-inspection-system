from __future__ import annotations

from urllib.parse import urljoin

from playwright.sync_api import Page


def goto(page: Page, base_url: str, path: str) -> None:
    page.goto(urljoin(f"{base_url}/", path.lstrip("/")), wait_until="networkidle")
