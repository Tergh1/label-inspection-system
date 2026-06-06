from __future__ import annotations

from playwright.sync_api import expect

from core.pages.base_page import BasePage


class HomePage(BasePage):
    def open(self) -> None:
        self.goto("/")

    def expect_public_actions(self) -> None:
        expect(self.by_test_id("home-sign-in-link")).to_be_visible()
        expect(self.by_test_id("home-register-link")).to_be_visible()

    def expect_authenticated_actions(self) -> None:
        expect(self.by_test_id("home-upload-template-link")).to_be_visible()
        expect(self.by_test_id("home-upload-image-link")).to_be_visible()
        expect(self.by_test_id("home-inspected-images-link")).to_be_visible()
