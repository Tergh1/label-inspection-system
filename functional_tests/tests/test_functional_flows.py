from __future__ import annotations

import re
from pathlib import Path

from playwright.sync_api import Page, expect
from pytest_bdd import given, parsers, scenarios, then, when

from core.config.settings import Settings
from core.models.inspection_data import ImageUploadData, TemplateUploadData
from core.models.scenario_context import ScenarioContext
from core.models.test_user import TestUser as UserCredentials
from core.pages.auth_page import AuthPage
from core.pages.base_page import BasePage
from core.pages.home_page import HomePage
from core.pages.images_page import ImagesPage
from core.pages.reports_page import ReportsPage
from core.pages.templates_page import TemplatesPage
from core.pages.upload_image_page import UploadImagePage
from core.pages.upload_template_page import UploadTemplatePage
from core.utils.files import sample_file


scenarios(
    "authentication.feature",
    "inspected_images.feature",
    "navigation_and_authorization.feature",
    "reports.feature",
    "templates.feature",
    "upload_image.feature",
    "upload_template.feature",
)


def _auth(page: Page, settings: Settings) -> AuthPage:
    return AuthPage(page, settings.base_url)


def _template_data(scenario_context: ScenarioContext) -> TemplateUploadData:
    assert scenario_context.template_name is not None
    return TemplateUploadData(
        friendly_name=scenario_context.template_name,
        file_path=sample_file("template.png"),
    )


def _image_data(scenario_context: ScenarioContext) -> ImageUploadData:
    file_path = sample_file("template_defect.png")
    scenario_context.image_name = Path(file_path).name
    return ImageUploadData(file_path=file_path)


def _invalid_user(settings: Settings) -> UserCredentials:
    return UserCredentials(
        email=f"missing-user@{settings.test_user_domain}",
        password="NotThePassword123!",
    )


@given("I am logged in as a generated user")
def logged_in(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    assert scenario_context.user is not None
    auth_page = _auth(page, settings)
    auth_page.logout_if_authenticated()
    auth_page.ensure_logged_in(scenario_context.user)


@given("I am logged out")
def logged_out(page: Page, settings: Settings) -> None:
    auth_page = _auth(page, settings)
    auth_page.logout_if_authenticated()
    auth_page.expect_public_nav()


@then("I should see authenticated navigation")
def authenticated_navigation(page: Page, settings: Settings) -> None:
    _auth(page, settings).expect_authenticated_nav()


@then("I should see public navigation")
def public_navigation(page: Page, settings: Settings) -> None:
    _auth(page, settings).expect_public_nav()


@when("I log out")
def log_out(page: Page, settings: Settings) -> None:
    _auth(page, settings).logout()


@when("I try to log in with invalid credentials")
def invalid_login(page: Page, settings: Settings) -> None:
    _auth(page, settings).login(_invalid_user(settings))


@then("I should see an invalid login error")
def invalid_login_error(page: Page, settings: Settings) -> None:
    _auth(page, settings).expect_invalid_login_error()


@when("I open the home page")
def open_home(page: Page, settings: Settings) -> None:
    HomePage(page, settings.base_url).open()


@then("I should see public home actions")
def public_home_actions(page: Page, settings: Settings) -> None:
    HomePage(page, settings.base_url).expect_public_actions()


@when(parsers.parse('I open protected page "{path}"'))
def open_protected_page(page: Page, settings: Settings, path: str) -> None:
    BasePage(page, settings.base_url).goto(path)


@then("I should be redirected to login")
def redirected_to_login(page: Page) -> None:
    expect(page).to_have_url(re.compile(r"/Account/Login"))


@given("I have uploaded a template")
@when("I upload a valid template")
def upload_template(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    upload_page = UploadTemplatePage(page, settings.base_url)
    upload_page.open()
    upload_page.upload(_template_data(scenario_context))
    upload_page.expect_uploaded()


@then("the template upload should succeed")
def template_upload_succeeds(page: Page, settings: Settings) -> None:
    UploadTemplatePage(page, settings.base_url).expect_uploaded()


@then("the template should appear in Templates")
def template_appears(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    assert scenario_context.template_name is not None
    templates_page = TemplatesPage(page, settings.base_url)
    templates_page.open()
    templates_page.expect_row(scenario_context.template_name)


@when("I submit the template form without a file")
def submit_template_without_file(page: Page, settings: Settings) -> None:
    upload_page = UploadTemplatePage(page, settings.base_url)
    upload_page.open()
    upload_page.submit_without_file()


@when("I submit the template form without a friendly name")
def submit_template_without_name(page: Page, settings: Settings) -> None:
    upload_page = UploadTemplatePage(page, settings.base_url)
    upload_page.open()
    upload_page.submit_without_friendly_name(sample_file("template.png"))


@when("I submit the template form with invalid tolerance")
def submit_template_with_invalid_tolerance(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.template_name is not None
    upload_page = UploadTemplatePage(page, settings.base_url)
    upload_page.open()
    upload_page.submit_with_invalid_tolerance(sample_file("template.png"), scenario_context.template_name)


@then(parsers.parse('I should see a template upload error containing "{text}"'))
def template_upload_error(page: Page, settings: Settings, text: str) -> None:
    UploadTemplatePage(page, settings.base_url).expect_error(text)


@when("I open the templates page")
def open_templates(page: Page, settings: Settings) -> None:
    TemplatesPage(page, settings.base_url).open()


@then("I should see the templates empty state")
def templates_empty(page: Page, settings: Settings) -> None:
    TemplatesPage(page, settings.base_url).expect_empty()


@when("I open the template row actions")
def open_template_actions(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    assert scenario_context.template_name is not None
    templates_page = TemplatesPage(page, settings.base_url)
    templates_page.open()
    templates_page.open_row_actions(scenario_context.template_name)


@given("I remove the existing template")
@when("I delete the template")
def delete_template(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    assert scenario_context.template_name is not None
    templates_page = TemplatesPage(page, settings.base_url)
    templates_page.open()
    templates_page.delete_template(scenario_context.template_name)


@then("the template deletion should succeed")
def template_deletion_succeeds(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.template_name is not None
    expect(TemplatesPage(page, settings.base_url).row(scenario_context.template_name)).to_be_hidden()


@when("I upload a valid inspection image")
def upload_image(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    upload_page = UploadImagePage(page, settings.base_url)
    upload_page.open()
    upload_page.upload(_image_data(scenario_context))


@given("I have uploaded and completed an inspection image")
def upload_completed_image(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    upload_image(page, settings, scenario_context)
    image_upload_queued(page, settings)
    image_completed_with_decision(page, settings, scenario_context)


@then("the image upload should be queued")
def image_upload_queued(page: Page, settings: Settings) -> None:
    UploadImagePage(page, settings.base_url).expect_uploaded_or_queued()


@then("the inspected image should complete with an inspection decision")
def image_completed_with_decision(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.image_name is not None
    images_page = ImagesPage(page, settings.base_url)
    images_page.open()
    images_page.wait_for_completed_decision(scenario_context.image_name, settings.ml_timeout_ms)


@when("I open the inspected images page")
def open_images(page: Page, settings: Settings) -> None:
    ImagesPage(page, settings.base_url).open()


@then("the inspected image should show similarity and decision fields")
def image_shows_decision_fields(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.image_name is not None
    images_page = ImagesPage(page, settings.base_url)
    images_page.expect_row(scenario_context.image_name)
    images_page.wait_for_completed_decision(scenario_context.image_name, settings.ml_timeout_ms)


@then("I can show defects when available")
def show_defects_when_available(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.image_name is not None
    images_page = ImagesPage(page, settings.base_url)
    if images_page.show_defects_if_available(scenario_context.image_name):
        images_page.by_test_id("defects-modal-close-button").click()
        expect(images_page.by_test_id("defects-modal")).to_be_hidden()


@when("I delete the inspected image")
def delete_image(page: Page, settings: Settings, scenario_context: ScenarioContext) -> None:
    assert scenario_context.image_name is not None
    ImagesPage(page, settings.base_url).delete_image(scenario_context.image_name)


@then("the image deletion should succeed")
def image_deletion_succeeds(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.image_name is not None
    expect(ImagesPage(page, settings.base_url).row(scenario_context.image_name)).to_be_hidden()


@when("I inspect again if the app allows it")
def inspect_again_conditionally(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.image_name is not None
    images_page = ImagesPage(page, settings.base_url)
    images_page.open()
    scenario_context.inspect_again_allowed = images_page.inspect_again_if_allowed(scenario_context.image_name)


@then("the inspect-again action should be conditionally handled")
def inspect_again_handled(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.image_name is not None
    assert isinstance(getattr(scenario_context, "inspect_again_allowed", None), bool)
    ImagesPage(page, settings.base_url).expect_row(scenario_context.image_name)


@then("I should see the inspected images empty state")
def images_empty(page: Page, settings: Settings) -> None:
    ImagesPage(page, settings.base_url).expect_empty()


@when("I submit the image upload form without an image")
def submit_image_without_image(page: Page, settings: Settings) -> None:
    upload_page = UploadImagePage(page, settings.base_url)
    upload_page.open()
    upload_page.submit_without_image()


@when("I open the image upload page")
def open_image_upload(page: Page, settings: Settings) -> None:
    UploadImagePage(page, settings.base_url).open()


@then("I should see the no-template warning")
def no_template_warning(page: Page, settings: Settings) -> None:
    UploadImagePage(page, settings.base_url).expect_no_template_alert()


@when("I submit the image upload form with invalid tolerance")
def submit_image_with_invalid_tolerance(page: Page, settings: Settings) -> None:
    upload_page = UploadImagePage(page, settings.base_url)
    upload_page.open()
    upload_page.submit_with_invalid_tolerance(sample_file("template_defect.png"))


@then(parsers.parse('I should see an image upload error containing "{text}"'))
def image_upload_error(page: Page, settings: Settings, text: str) -> None:
    UploadImagePage(page, settings.base_url).expect_error(text)


@when("I open the reports page")
def open_reports(page: Page, settings: Settings) -> None:
    ReportsPage(page, settings.base_url).open()


@then("I should see the reports empty state")
def reports_empty(page: Page, settings: Settings) -> None:
    ReportsPage(page, settings.base_url).expect_empty()


@then("the inspected image should appear in reports and statistics")
def report_contains_image(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    assert scenario_context.image_name is not None
    ReportsPage(page, settings.base_url).expect_row(scenario_context.image_name)


@when("I apply and clear report filters")
def apply_and_clear_report_filters(page: Page, settings: Settings) -> None:
    reports_page = ReportsPage(page, settings.base_url)
    reports_page.open()
    reports_page.apply_completed_filter()
    reports_page.clear_filters()


@then("the report filters should be cleared")
def report_filters_cleared(page: Page) -> None:
    assert "processingStatus" not in page.url


@when("I download the CSV and XLSX reports")
def download_reports(
    page: Page,
    settings: Settings,
    scenario_context: ScenarioContext,
) -> None:
    reports_page = ReportsPage(page, settings.base_url)
    reports_page.open()
    for fmt in ("csv", "xlsx"):
        download = reports_page.download_export(fmt)
        assert download.failure() is None
        assert download.path() is not None
        scenario_context.downloaded_files[fmt] = download.suggested_filename


@then("both report exports should be downloaded")
def report_exports_downloaded(scenario_context: ScenarioContext) -> None:
    assert scenario_context.downloaded_files["csv"].endswith(".csv")
    assert scenario_context.downloaded_files["xlsx"].endswith(".xlsx")
