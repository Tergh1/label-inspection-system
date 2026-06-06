from __future__ import annotations

from playwright.sync_api import Download, Locator, expect

from core.pages.base_page import BasePage


class ReportsPage(BasePage):
    def open(self) -> None:
        self.goto("/reports")

    def row(self, image_name: str) -> Locator:
        return self.by_test_id("report-row").filter(has_text=image_name).first

    def expect_empty(self) -> None:
        expect(self.by_test_id("reports-empty-state")).to_be_visible()

    def expect_row(self, image_name: str) -> None:
        expect(self.row(image_name)).to_be_visible()
        expect(self.by_test_id("reports-statistics")).to_be_visible()

    def apply_completed_filter(self) -> None:
        self.by_test_id("reports-processing-filter").select_option("Completed")
        self.by_test_id("reports-apply-filters-button").click()
        self.page.wait_for_load_state("networkidle")
        assert "processingStatus=Completed" in self.page.url

    def clear_filters(self) -> None:
        self.by_test_id("reports-clear-filters-link").click()
        self.page.wait_for_load_state("networkidle")
        assert "processingStatus" not in self.page.url

    def download_export(self, fmt: str) -> Download:
        link_id = "reports-export-csv-link" if fmt == "csv" else "reports-export-xlsx-link"
        with self.page.expect_download() as download_info:
            self.by_test_id(link_id).click()
        return download_info.value
