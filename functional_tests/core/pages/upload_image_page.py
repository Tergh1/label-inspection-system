from __future__ import annotations

from playwright.sync_api import Locator, expect

from core.models.inspection_data import ImageUploadData
from core.pages.base_page import BasePage


class UploadImagePage(BasePage):
    def open(self) -> None:
        self.goto("/images/upload")

    def upload(self, data: ImageUploadData) -> None:
        self.by_test_id("image-file-input").set_input_files(data.file_path)
        self.latest_draft_row().get_by_test_id("image-upload-draft-tolerance-input").fill(data.tolerance_percent)
        self.latest_draft_row().get_by_test_id("image-upload-draft-description-input").fill(data.description)
        self.by_test_id("image-upload-submit-button").click()

    def submit_without_image(self) -> None:
        self.by_test_id("image-upload-submit-button").click()

    def submit_with_invalid_tolerance(self, file_path: str) -> None:
        self.by_test_id("image-file-input").set_input_files(file_path)
        self.latest_draft_row().get_by_test_id("image-upload-draft-tolerance-input").fill("101")
        self.by_test_id("image-upload-submit-button").click()

    def latest_draft_row(self) -> Locator:
        return self.by_test_id("image-upload-draft-row").last

    def expect_no_template_alert(self) -> None:
        expect(self.by_test_id("image-upload-no-template-alert")).to_be_visible()

    def expect_uploaded_or_queued(self) -> None:
        expect(self.by_test_id("image-upload-status")).to_contain_text("queued")

    def expect_error(self, text: str) -> None:
        expect(self.page.locator("[role='alert'], .validation-message, .text-danger")).to_contain_text(text)
    
    def expect_uploaded_image_tolerance_error(self, text: str) -> None:
        tolerance_locator = self.page.get_by_test_id("image-upload-draft-tolerance-input")
        expect(tolerance_locator).to_be_visible(timeout=20000)
        error_message = tolerance_locator.evaluate("el => el.validationMessage")

        assert error_message == text, f"Expected tolerance input error message to be '{text}', but got '{error_message}'"