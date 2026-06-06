from __future__ import annotations

from playwright.sync_api import Page, expect

from core.models.test_user import TestUser
from core.pages.base_page import BasePage


class AuthPage(BasePage):
    def __init__(self, page: Page, base_url: str) -> None:
        super().__init__(page, base_url)

    def open_login(self) -> None:
        self.goto("/Account/Login")

    def open_register(self) -> None:
        self.goto("/Account/Register")

    def login(self, user: TestUser) -> None:
        self.open_login()
        self.by_test_id("login-email-input").fill(user.email)
        self.by_test_id("login-password-input").fill(user.password)
        self.by_test_id("login-submit-button").click()
        self.page.wait_for_load_state("networkidle")

    def register(self, user: TestUser) -> None:
        self.open_register()
        self.by_test_id("register-email-input").fill(user.email)
        self.by_test_id("register-password-input").fill(user.password)
        self.by_test_id("register-confirm-password-input").fill(user.password)
        self.by_test_id("register-submit-button").click()
        self.page.wait_for_load_state("networkidle")

    def ensure_logged_in(self, user: TestUser) -> None:
        self.login(user)
        if self.is_logged_in():
            return

        self.register(user)
        self.confirm_pending_account_if_prompted()
        if not self.is_logged_in():
            self.login(user)

        self.expect_authenticated_nav()

    def confirm_pending_account_if_prompted(self) -> None:
        confirm_link = self.by_test_id("register-confirm-account-link")
        if confirm_link.count() == 0 or not confirm_link.first.is_visible():
            return

        confirm_link.first.click()
        self.page.wait_for_load_state("networkidle")

    def logout_if_authenticated(self) -> None:
        self.goto("/")
        logout_button = self.by_test_id("nav-logout-button")
        if logout_button.count() > 0 and logout_button.first.is_visible():
            logout_button.click()
            self.page.wait_for_load_state("networkidle")

    def logout(self) -> None:
        self.by_test_id("nav-logout-button").click()
        self.page.wait_for_load_state("networkidle")

    def is_logged_in(self) -> bool:
        account_link = self.by_test_id("nav-account-link")
        return account_link.count() > 0 and account_link.first.is_visible()

    def expect_authenticated_nav(self) -> None:
        expect(self.by_test_id("nav-templates-link")).to_be_visible()
        expect(self.by_test_id("nav-images-link")).to_be_visible()
        expect(self.by_test_id("nav-reports-link")).to_be_visible()
        expect(self.by_test_id("nav-logout-button")).to_be_visible()

    def expect_public_nav(self) -> None:
        expect(self.by_test_id("nav-login-link")).to_be_visible()
        expect(self.by_test_id("nav-register-link")).to_be_visible()

    def expect_invalid_login_error(self) -> None:
        expect(self.by_test_id("account-status-message")).to_contain_text("Invalid login attempt")
