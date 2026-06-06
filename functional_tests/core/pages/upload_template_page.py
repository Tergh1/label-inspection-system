from __future__ import annotations

from playwright.sync_api import expect

from core.models.inspection_data import TemplateUploadData
from core.pages.base_page import BasePage


class UploadTemplatePage(BasePage):
    def open(self) -> None:
        self.goto("/templates/upload")

    def upload(self, data: TemplateUploadData) -> None:
        self.by_test_id("template-file-input").set_input_files(data.file_path)
        self.by_test_id("template-friendly-name-input").fill(data.friendly_name)
        self.by_test_id("template-tolerance-input").fill(data.tolerance_percent)
        self.by_test_id("template-description-input").fill(data.description)
        self.by_test_id("template-upload-submit-button").click()

    def submit_without_file(self, friendly_name: str = "Missing file template") -> None:
        self.by_test_id("template-friendly-name-input").fill(friendly_name)
        self.by_test_id("template-tolerance-input").fill("20")
        self.by_test_id("template-upload-submit-button").click()

    def submit_without_friendly_name(self, file_path: str) -> None:
        self.by_test_id("template-file-input").set_input_files(file_path)
        self.by_test_id("template-friendly-name-input").fill("")
        self.by_test_id("template-upload-submit-button").click()

    def submit_with_invalid_tolerance(self, file_path: str, friendly_name: str) -> None:
        self.by_test_id("template-file-input").set_input_files(file_path)
        self.by_test_id("template-friendly-name-input").fill(friendly_name)
        self.by_test_id("template-tolerance-input").fill("101")
        self.by_test_id("template-upload-submit-button").click()

    def expect_uploaded(self) -> None:
        expect(self.by_test_id("template-upload-status")).to_contain_text("Template uploaded")

    def expect_error(self, text: str) -> None:
        expect(self.page.locator("[role='alert'], .validation-message, .text-danger")).to_contain_text(text)
