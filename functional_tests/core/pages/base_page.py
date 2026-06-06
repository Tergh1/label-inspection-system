from __future__ import annotations

from playwright.sync_api import Locator, Page, expect


class BasePage:
    def __init__(self, page: Page, base_url: str) -> None:
        self.page = page
        self.base_url = base_url.rstrip("/")

    def goto(self, path: str) -> None:
        self.page.goto(f"{self.base_url}/{path.lstrip('/')}", wait_until="networkidle")

    def by_test_id(self, test_id: str) -> Locator:
        return self.page.get_by_test_id(test_id)

    def expect_url_contains(self, fragment: str) -> None:
        assert fragment in self.page.url
