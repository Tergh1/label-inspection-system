# Functional Tests

Standalone Python 3.11 + Poetry BDD tests for the Blazor label-inspection app.

The Blazor app and ML service must already be running. The default app URL is
`http://localhost:7108`; override it with `BASE_URL`.

```bash
cd functional_tests
poetry install
poetry run playwright install chromium
BASE_URL=http://localhost:7108 poetry run pytest
poetry run pytest --html=reports/functional-report.html --self-contained-html
```

The tests use only explicit `data-testid` attributes for stable selectors.
