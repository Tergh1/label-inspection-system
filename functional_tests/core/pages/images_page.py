from __future__ import annotations

from playwright.sync_api import Locator, expect

from core.enums.outcome_status import OutcomeStatus
from core.enums.processing_status import ProcessingStatus
from core.pages.base_page import BasePage
from core.utils.waits import wait_for_processing_complete


class ImagesPage(BasePage):
    def open(self) -> None:
        self.goto("/images")

    def row(self, image_name: str) -> Locator:
        return self.by_test_id("image-row").filter(has_text=image_name).first

    def expect_row(self, image_name: str) -> None:
        expect(self.row(image_name)).to_be_visible()

    def expect_empty(self) -> None:
        expect(self.by_test_id("images-empty-state")).to_be_visible()

    def wait_for_completed_decision(self, image_name: str, timeout_ms: int) -> str:
        row = self.row(image_name)
        expect(row).to_be_visible(timeout=timeout_ms)
        status = wait_for_processing_complete(row.get_by_test_id("image-row-processing-status"), timeout_ms)
        assert status == ProcessingStatus.COMPLETED

        outcome = row.get_by_test_id("image-row-outcome-status").inner_text().strip()
        assert outcome in {value.value for value in OutcomeStatus if value != OutcomeStatus.PENDING}
        expect(row.get_by_test_id("image-row-similarity")).not_to_contain_text("N/A")
        return outcome

    def open_row_actions(self, image_name: str) -> None:
        row = self.row(image_name)
        row.get_by_test_id("image-row-actions-button").click()
        expect(row.get_by_test_id("image-row-menu")).to_be_visible()

    def show_defects_if_available(self, image_name: str) -> bool:
        row = self.row(image_name)
        self.open_row_actions(image_name)
        button = row.get_by_test_id("image-show-defects-menuitem")
        if button.count() == 0:
            return False

        button.click()
        expect(self.by_test_id("defects-modal")).to_be_visible()
        return True

    def inspect_again_if_allowed(self, image_name: str) -> bool:
        row = self.row(image_name)
        self.open_row_actions(image_name)
        button = row.get_by_test_id("image-inspect-again-menuitem")
        if button.count() == 0:
            return False

        button.click()
        expect(self.by_test_id("images-status")).to_contain_text("queued")
        return True

    def delete_image(self, image_name: str) -> None:
        row = self.row(image_name)
        self.open_row_actions(image_name)
        row.get_by_test_id("image-delete-menuitem").click()
        expect(self.by_test_id("image-delete-modal")).to_be_visible()
        self.by_test_id("image-delete-confirm-button").click()
        expect(self.row(image_name)).to_be_hidden()
