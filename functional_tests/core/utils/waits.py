from __future__ import annotations

import re
import time

from playwright.sync_api import Locator, expect

from core.enums.processing_status import IN_FLIGHT_STATUSES


def wait_for_non_empty(locator: Locator, timeout_ms: int) -> None:
    expect(locator).not_to_have_text("", timeout=timeout_ms)


def wait_for_processing_complete(status_locator: Locator, timeout_ms: int) -> str:
    deadline = time.monotonic() + (timeout_ms / 1000)

    while time.monotonic() < deadline:
        status = status_locator.inner_text().strip()
        if status not in {value.value for value in IN_FLIGHT_STATUSES}:
            return status

        status_locator.page.wait_for_timeout(250)

    expect(status_locator).not_to_have_text(
        re.compile("|".join(value.value for value in IN_FLIGHT_STATUSES)),
        timeout=1,
    )
    return status_locator.inner_text().strip()
