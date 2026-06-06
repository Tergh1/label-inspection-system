from __future__ import annotations

from playwright.sync_api import Locator, expect

from core.pages.base_page import BasePage


class TemplatesPage(BasePage):
    def open(self) -> None:
        self.goto("/templates")

    def row(self, template_name: str) -> Locator:
        return self.by_test_id("template-row").filter(has_text=template_name).first

    def expect_row(self, template_name: str) -> None:
        expect(self.row(template_name)).to_be_visible()

    def expect_empty(self) -> None:
        expect(self.by_test_id("templates-empty-state")).to_be_visible()

    def open_row_actions(self, template_name: str) -> None:
        row = self.row(template_name)
        row.get_by_test_id("template-row-actions-button").click()
        expect(row.get_by_test_id("template-row-menu")).to_be_visible()

    def delete_template(self, template_name: str) -> None:
        row = self.row(template_name)
        self.open_row_actions(template_name)
        row.get_by_test_id("template-delete-menuitem").click()
        expect(self.by_test_id("template-delete-modal")).to_be_visible()
        self.by_test_id("template-delete-confirm-button").click()
        expect(self.row(template_name)).to_be_hidden()
