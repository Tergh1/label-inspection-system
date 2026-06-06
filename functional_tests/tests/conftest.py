from __future__ import annotations

import hashlib
import uuid

import pytest
from playwright.sync_api import Browser, Page, sync_playwright

from core.config.settings import Settings, load_settings
from core.models.scenario_context import ScenarioContext
from core.models.test_user import TestUser
from core.utils.files import slugify


@pytest.fixture(scope="session")
def settings() -> Settings:
    return load_settings()


@pytest.fixture(scope="session")
def browser(settings: Settings) -> Browser:
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(
            headless=settings.headless,
            slow_mo=settings.slow_mo_ms,
        )
        yield browser
        browser.close()


@pytest.fixture
def page(browser: Browser, settings: Settings) -> Page:
    context = browser.new_context(base_url=settings.base_url)
    page = context.new_page()
    page.set_default_timeout(settings.default_timeout_ms)
    yield page
    context.close()


@pytest.fixture
def scenario_context(request: pytest.FixtureRequest, settings: Settings) -> ScenarioContext:
    scenario_name = getattr(request.node, "name", "scenario")
    digest = hashlib.sha1(scenario_name.encode("utf-8")).hexdigest()[:10]
    run_id = uuid.uuid4().hex[:8]
    slug = slugify(scenario_name)

    return ScenarioContext(
        user=TestUser(
            email=f"bdd-{slug}-{digest}-{run_id}@{settings.test_user_domain}",
            password="Functional123!",
        ),
        template_name=f"BDD template {slug} {digest} {run_id}",
        image_name=None,
    )
