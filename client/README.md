# Client Project README

## Overview

The `client` project is the web application for the Label Inspection System. It is an ASP.NET Core Blazor Web App running on .NET 8 with:

- Blazor Server style interactive UI
- ASP.NET Core Identity for authentication
- Entity Framework Core with PostgreSQL
- local file storage for uploaded inspection images
- local file storage for uploaded inspection templates
- asynchronous communication with the Python ML service
- a webhook endpoint that receives ML inspection results

At a high level, the client does five things:

1. authenticates users
2. accepts template uploads and image uploads plus inspection metadata
3. stores template files, image files, and inspection records
4. dispatches work to the ML service
5. updates inspection records when the ML service calls back

---

## Technology and Versions

### Application framework

- Target framework: `.NET 8` (`net8.0`)
- Project SDK: `Microsoft.NET.Sdk.Web`
- UI model: `Blazor Web App` with interactive server components

### Data and authentication packages

Declared in [client.csproj](/Users/delkov/Projects/label-inspection-system/client/client.csproj):

- `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` `8.0.20`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` `8.0.20`
- `Microsoft.EntityFrameworkCore.Tools` `8.0.20`
- `Npgsql.EntityFrameworkCore.PostgreSQL` `8.0.8`

### Local SDK/tooling notes

- The project targets `.NET 8`, so use a `.NET 8 SDK` for development and CI.
- This machine currently has `dotnet 10.0.102` installed, which can build .NET 8 projects, but the application target remains `.NET 8`.
- EF CLI tooling is pinned in the repository root [dotnet-tools.json](/Users/delkov/Projects/label-inspection-system/dotnet-tools.json) as `dotnet-ef 8.0.20`.

---

## Project Responsibilities

The client project is responsible for:

- user registration and login
- storing application users in PostgreSQL
- saving uploaded template files on disk
- saving uploaded image files on disk
- creating template records in PostgreSQL
- creating inspection records in PostgreSQL
- exposing public URLs for stored images and templates so the ML service can fetch them
- dispatching inspection jobs to the ML service
- receiving webhook updates from the ML service
- calculating the final acceptance state from similarity and defect data
- presenting uploaded templates to the signed-in user
- presenting uploaded inspection history to the signed-in user

The client project does not:

- run ML inference
- directly process image similarity
- directly detect defects

Those responsibilities belong to the Python ML service under `ml_service`.

---

## Runtime Architecture

### Main components

- `Blazor UI`: authenticated pages for template management, upload, and review
- `Application services`: orchestration for file storage, URL generation, and ML dispatch
- `PostgreSQL`: stores users and inspection metadata
- `Local file storage`: stores uploaded files under `App_Data/...`
- `ML service`: receives inspection requests and sends webhook results back

### Request/processing lifecycle

1. A signed-in user uploads a template in `/templates/upload`.
2. The client validates file type, size, friendly name, and default tolerance.
3. The template file is stored on disk and a row is created in `InspectionTemplates`.
4. Later, the user uploads one or more images in `/images/upload`.
5. The image upload requires a template selection from the current user's uploaded templates.
6. The selected template is shared across the batch, and each image gets its own tolerance and optional description.
7. Each image file is stored on disk and a row is created in `InspectionImages` linked through `TemplateId`.
8. For each image, the client builds:
   - a public image URL for the stored file
   - a public template URL for the selected template
   - a webhook callback URL for ML results
9. The client sends a JSON `POST` request to the ML service for each image, one at a time.
10. Each inspection record moves to `Queued` if dispatch succeeds, or `Failed` if dispatch fails.
11. The ML service processes each image asynchronously against the selected template.
12. The ML service posts results to the client webhook.
13. The client updates each inspection row with similarity, defects, status, and outcome.

---

## Local Development Setup

### Prerequisites

- `.NET 8 SDK`
- Docker Desktop or compatible Docker engine
- PostgreSQL running locally, preferably through the repository compose file

### Database setup with Docker Compose

The repository root contains [docker-compose.yml](/Users/delkov/Projects/label-inspection-system/docker-compose.yml) for local PostgreSQL.

Start PostgreSQL from the repository root:

```bash
docker compose up -d postgres
```

What this starts:

- PostgreSQL 16
- exposed on `localhost:5432`
- database `label_inspection_identity_dev`
- database `label_inspection_identity`
- persistent Docker volume `postgres_data`

Optional admin UI:

```bash
docker compose --profile tools up -d pgadmin
```

pgAdmin defaults:

- URL: `http://localhost:5050`
- email: `admin@example.com`
- password: from `PGADMIN_DEFAULT_PASSWORD` or fallback placeholder in compose

### Important password note

The compose file now uses placeholders:

- `POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-CHANGE_ME}`
- `PGADMIN_DEFAULT_PASSWORD=${PGADMIN_DEFAULT_PASSWORD:-CHANGE_ME}`

That means one of the following must be true before the app can connect:

- you export `POSTGRES_PASSWORD` to match the password in the client connection string
- or you change the client connection string to match the password you used for PostgreSQL

The current [appsettings.Development.json](/Users/delkov/Projects/label-inspection-system/client/appsettings.Development.json) contains a concrete local development password. In practice, this should be moved to user secrets or environment variables instead of remaining in source control.

### Apply database migrations

From the repository root:

```bash
dotnet tool restore
dotnet ef database update --project client
```

### Run the client

From the repository root:

```bash
dotnet run --project client
```

Local URLs are defined in [launchSettings.json](/Users/delkov/Projects/label-inspection-system/client/Properties/launchSettings.json):

- HTTP: `http://localhost:7107`
- HTTPS: `https://localhost:7107`

The development configuration expects the public app base URL to be:

- `https://localhost:7107`

This value is important because the ML service uses it to call back into the webhook and to fetch the uploaded image file.

---

## Configuration

The client uses:

- [appsettings.json](/Users/delkov/Projects/label-inspection-system/client/appsettings.json)
- [appsettings.Development.json](/Users/delkov/Projects/label-inspection-system/client/appsettings.Development.json)

### `ConnectionStrings`

#### `DefaultConnection`

PostgreSQL connection string used by Entity Framework Core and ASP.NET Identity.

Example shape:

```text
Host=localhost;Port=5432;Database=label_inspection_identity_dev;Username=postgres;Password=...
```

Purpose:

- stores users
- stores inspection metadata
- stores workflow state

### `InspectionStorage`

Bound to [InspectionStorageOptions.cs](/Users/delkov/Projects/label-inspection-system/client/Options/InspectionStorageOptions.cs).

#### `UploadRoot`

Directory where uploaded files are persisted.

Examples:

- production/default: `App_Data/uploads`
- development: `App_Data/uploads-dev`

Behavior:

- relative paths are resolved against the application content root
- the application creates the directory if it does not exist

#### `PublicFilePathPrefix`

Route prefix used to expose stored files over HTTP.

Current value:

- `/inspection-files`

Example generated public URL:

```text
https://localhost:7107/inspection-files/{publicAccessToken}
```

#### `TemplatePublicFilePathPrefix`

Route prefix used to expose stored template files over HTTP.

Current value:

- `/inspection-template-files`

Example generated public URL:

```text
https://localhost:7107/inspection-template-files/{publicAccessToken}
```

#### `MaxFileSizeBytes`

Maximum allowed upload size.

Current value:

- `10485760` bytes
- effectively `10 MB`

#### `AllowedExtensions`

Allowed file extensions for uploads.

Current defaults:

- `.png`
- `.jpg`
- `.jpeg`
- `.bmp`
- `.webp`

Validation is extension-based, and the content type must also start with `image/` when present.

### `InspectionMl`

Bound to [InspectionMlOptions.cs](/Users/delkov/Projects/label-inspection-system/client/Options/InspectionMlOptions.cs).

#### `BaseUrl`

Base URL of the ML service.

Current default:

- `http://localhost:8000`

#### `InspectPath`

Relative path for job dispatch to the ML service.

Current default:

- `/inspect-async`

#### `ApiKey`

Shared secret sent from the client to the ML service when dispatching inspection jobs.

This value is attached to the outgoing request only when non-empty.

#### `ApiKeyHeaderName`

Header name used when sending the ML API key.

Current default:

- `X-API-KEY`

#### `PublicAppBaseUrl`

Publicly reachable base URL of the client application.

This value is used to generate:

- the public image URL for the uploaded file
- the webhook URL for ML callbacks

Important:

- this must be reachable by the ML service
- if the ML service runs in another container or another machine, `localhost` may be wrong

#### `WebhookPath`

The route path exposed by the client for ML callbacks.

Current value:

- `/api/ml/webhook`

#### `WebhookSecret`

Shared secret expected on incoming webhook requests.

If the header is missing or the value does not match, the client returns `401 Unauthorized`.

#### `WebhookSecretHeaderName`

Header name used to validate webhook requests.

Current default:

- `X-Webhook-Secret`

### Logging

Standard ASP.NET Core logging configuration.

Current defaults:

- `Default`: `Information`
- `Microsoft.AspNetCore`: `Warning`

### `AllowedHosts`

Standard ASP.NET Core host filtering setting.

Current default:

- `*`

---

## Dependency Injection and Startup

Startup is configured in [Program.cs](/Users/delkov/Projects/label-inspection-system/client/Program.cs).

### Registered framework services

- Razor components with interactive server rendering
- ASP.NET Core authentication cookies
- ASP.NET Core Identity
- cascading authentication state
- EF Core `ApplicationDbContext`

### Registered application services

- `InspectionWorkflowService`
- `InspectionFileStorage`
- `InspectionUrlBuilder`
- `MlInspectionClient`

### HTTP pipeline

In development:

- migrations endpoint is enabled with `UseMigrationsEndPoint()`

In non-development:

- exception handler is enabled
- HSTS is enabled

Always enabled:

- HTTPS redirection
- static files
- antiforgery
- Razor components
- identity endpoints
- inspection file endpoint
- ML webhook endpoint

---

## Project Structure

### Top-level layout

```text
client/
├── Components/
│   ├── Account/
│   ├── Layout/
│   └── Pages/
├── Contracts/
├── Data/
│   ├── Configurations/
│   ├── Entities/
│   └── Migrations/
├── Endpoints/
├── Options/
├── Properties/
├── Services/
├── wwwroot/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── client.csproj
```

### Folder responsibilities

#### `Components/`

Blazor UI and Identity UI integration.

- `Pages/Home.razor`: landing page
- `Pages/UploadImage.razor`: image upload form
- `Pages/Images.razor`: inspection listing and actions
- `Account/*`: authentication and account management UI

#### `Contracts/`

Types used for external message binding.

- `MlWebhookResult.cs`: payload received from the ML service webhook

#### `Data/`

Persistence layer.

- `ApplicationDbContext.cs`: EF Core DbContext
- `ApplicationUser.cs`: Identity user entity
- `Entities/InspectionTemplate.cs`: uploaded template metadata
- `Entities/InspectionImage.cs`: inspection aggregate record
- `Configurations/InspectionTemplateConfiguration.cs`: EF mapping for templates
- `Configurations/InspectionImageConfiguration.cs`: EF mapping
- `Migrations/*`: database schema history

#### `Endpoints/`

Minimal API endpoints that are not Blazor pages.

- `InspectionFileEndpoints.cs`: serves stored image and template files by tokenized URL
- `MlWebhookEndpoints.cs`: receives asynchronous ML result callbacks

#### `Options/`

Strongly typed configuration classes.

- `InspectionStorageOptions.cs`
- `InspectionMlOptions.cs`

#### `Services/`

Application orchestration and infrastructure logic.

- `InspectionWorkflowService.cs`: main inspection workflow
- `InspectionTemplateWorkflowService.cs`: template create/list/delete workflow
- `InspectionFileStorage.cs`: file validation and persistence
- `InspectionUrlBuilder.cs`: public URL generation
- `MlInspectionClient.cs`: outbound HTTP client to the ML service

#### `wwwroot/`

Static assets such as CSS, Bootstrap, and favicon.

---

## Data Model

### `ApplicationUser`

Defined in [ApplicationUser.cs](/Users/delkov/Projects/label-inspection-system/client/Data/ApplicationUser.cs).

Currently this inherits directly from `IdentityUser` and adds no extra profile fields.

### `InspectionTemplate`

Defined in [InspectionTemplate.cs](/Users/delkov/Projects/label-inspection-system/client/Data/Entities/InspectionTemplate.cs).

Fields:

- `Id`: template identifier
- `OwnerUserId`: owning authenticated user
- `FriendlyName`: required user-visible name shown in dropdowns and listings
- `OriginalFileName`: original uploaded file name
- `ContentType`: MIME type
- `FileSizeBytes`: stored file size
- `StoredRelativePath`: relative path on disk below upload root
- `PublicAccessToken`: token used to expose the file without revealing the physical path
- `Description`: optional template description
- `TolerancePercent`: default tolerance copied into the image upload form
- `CreatedAtUtc`: creation timestamp
- `UpdatedAtUtc`: last update timestamp

### `InspectionImage`

Defined in [InspectionImage.cs](/Users/delkov/Projects/label-inspection-system/client/Data/Entities/InspectionImage.cs).

Fields:

- `Id`: inspection identifier, also used as ML `image_id`
- `TemplateId`: optional foreign key to the selected template; nullable to support legacy rows created before template management existed
- `OwnerUserId`: owning authenticated user
- `OriginalFileName`: original uploaded file name
- `ContentType`: MIME type
- `FileSizeBytes`: stored file size
- `StoredRelativePath`: relative path on disk below upload root
- `PublicAccessToken`: token used to expose the file without revealing the physical path
- `Description`: optional user-entered description
- `TolerancePercent`: saved image tolerance, initially seeded from the selected template but still editable per image
- `MinimumSimilarityPercent`: computed as `100 - tolerance`
- `ProcessingStatus`: workflow state
- `OutcomeStatus`: business result state
- `SimilarityPercent`: numeric result returned by ML service
- `DefectsJson`: raw JSON defect payload stored as PostgreSQL `jsonb`
- `FailureReason`: dispatch or processing failure message
- `CreatedAtUtc`: creation timestamp
- `UpdatedAtUtc`: last update timestamp

### Enum: `ProcessingStatus`

Defined in [ProcessingStatus.cs](/Users/delkov/Projects/label-inspection-system/client/Data/Entities/ProcessingStatus.cs).

Values:

- `PendingDispatch`
- `Queued`
- `Processing`
- `Completed`
- `Failed`

### Enum: `OutcomeStatus`

Defined in [OutcomeStatus.cs](/Users/delkov/Projects/label-inspection-system/client/Data/Entities/OutcomeStatus.cs).

Values:

- `Pending`
- `Valid`
- `ValidWithDefects`
- `Invalid`

### Database mapping

Configured in [InspectionImageConfiguration.cs](/Users/delkov/Projects/label-inspection-system/client/Data/Configurations/InspectionImageConfiguration.cs).

Important mapping rules:

- table name: `InspectionTemplates`
- table name: `InspectionImages`
- `DefectsJson` stored as `jsonb`
- enum values persisted as strings
- decimal precision for tolerance/similarity fields is `numeric(5,2)`
- unique index on `PublicAccessToken`
- indexes on `OwnerUserId`, `CreatedAtUtc`, and `ProcessingStatus`
- `InspectionImage.TemplateId` references `InspectionTemplate.Id`

---

## File Storage Design

File storage behavior is implemented in [InspectionFileStorage.cs](/Users/delkov/Projects/label-inspection-system/client/Services/InspectionFileStorage.cs).

### Validation rules

- file size must be greater than `0`
- file size must not exceed `MaxFileSizeBytes`
- extension must be listed in `AllowedExtensions`
- content type must start with `image/` when present

### Storage pattern

Files are stored under:

```text
{UploadRoot}/{yyyy}/{MM}/{dd}/{generated-guid}.{ext}
```

Example:

```text
App_Data/uploads-dev/2026/03/10/228a5047433247cda2b6e22f9d47bae9.jpg
```

The database stores only the relative path and public access token, not the absolute path.

### Public file access

Files are served through a route, not directly via static-file directory browsing.

Route patterns:

```text
/inspection-files/{token}
/inspection-template-files/{token}
```

Lookup flow:

1. resolve `InspectionImage` or `InspectionTemplate` by `PublicAccessToken`
2. resolve stored file path
3. return the file if it exists
4. return `404` if the record or file does not exist

---

## Communication with the ML Service

### Outbound dispatch contract

Implemented in [MlInspectionClient.cs](/Users/delkov/Projects/label-inspection-system/client/Services/MlInspectionClient.cs).

The client sends:

- HTTP method: `POST`
- URL: `{BaseUrl}{InspectPath}`
- content type: `application/json`

JSON fields:

- `image_url`
- `template_url`
- `callback_url`
- `image_id`

Optional header:

- `{ApiKeyHeaderName}: {ApiKey}`

Example logical payload:

```text
POST http://localhost:8000/inspect-async
X-API-KEY: dev-ml-token

image_url=https://localhost:7107/inspection-files/{token}
template_url=https://localhost:7107/inspection-template-files/{token}
callback_url=https://localhost:7107/api/ml/webhook
image_id={guid}
```

### Inbound webhook contract

Implemented in [MlWebhookEndpoints.cs](/Users/delkov/Projects/label-inspection-system/client/Endpoints/MlWebhookEndpoints.cs) and bound to [MlWebhookResult.cs](/Users/delkov/Projects/label-inspection-system/client/Contracts/MlWebhookResult.cs).

Incoming fields:

- `image_id`
- `similarity_percent`
- `defects`
- `error`
- `failure_reason`
- `status`

The webhook route is configurable through `InspectionMl:WebhookPath`.

### Webhook authentication

The endpoint expects:

- header name from `WebhookSecretHeaderName`
- header value equal to `WebhookSecret`

If validation fails:

- response is `401 Unauthorized`

### Webhook processing rules

If `image_id` is not a valid GUID:

- response is `400 Bad Request`

If no matching `InspectionImage` exists:

- response is `404 Not Found`

If `failure_reason` or `error` is provided:

- `ProcessingStatus = Failed`
- `OutcomeStatus = Pending`
- `FailureReason = provided message`

If `status == "processing"`:

- `ProcessingStatus = Processing`

Otherwise the inspection is treated as completed:

- `ProcessingStatus = Completed`
- `SimilarityPercent = payload.similarity_percent`
- `DefectsJson = serialized payload.defects`

Outcome calculation:

- if similarity is below `MinimumSimilarityPercent`, outcome is `Invalid`
- if similarity meets threshold, outcome is `Valid`
- if defects are present, outcome is forced to `ValidWithDefects`

Current behavior note:

- `ValidWithDefects` overrides both `Valid` and `Invalid` when `defects` are present, so defects currently take precedence over similarity failure in the stored outcome.

---

## Application Workflow Service

The main orchestration logic is in [InspectionWorkflowService.cs](/Users/delkov/Projects/label-inspection-system/client/Services/InspectionWorkflowService.cs).

### `CreateAsync(...)`

Responsibilities:

- validates tolerance range `0..100`
- validates that the selected template belongs to the current user
- stores uploaded file
- creates `InspectionImage`
- computes `MinimumSimilarityPercent = 100 - TolerancePercent`
- saves the inspection record
- dispatches the inspection to the ML service

Initial record values:

- `ProcessingStatus = PendingDispatch`
- `OutcomeStatus = Pending`

After dispatch:

- on success: `ProcessingStatus = Queued`
- on failure: `ProcessingStatus = Failed` and `FailureReason = exception message`

### `GetUserImagesAsync(...)`

Returns all inspections for the current user, newest first.

### `DeleteAsync(...)`

- verifies ownership
- deletes the database row
- deletes the stored file from disk

### `RedispatchFailedAsync(...)`

- only allowed when status is `Failed`
- clears previous similarity/defect/failure state
- resets processing to `PendingDispatch`
- redispatches to the ML service

---

## UI Pages and User Experience

### Home page

Defined in [Home.razor](/Users/delkov/Projects/label-inspection-system/client/Components/Pages/Home.razor).

Provides:

- introduction to the portal
- sign-in / register actions for anonymous users
- upload / view actions for authenticated users
- a short explanation of upload contract, async processing, and tolerance logic

### Upload page

Defined in [UploadImage.razor](/Users/delkov/Projects/label-inspection-system/client/Components/Pages/UploadImage.razor).

Route:

- `/images/upload`

Authorization:

- requires authenticated user

Inputs:

- one or more image files
- template selection
- per-image tolerance percent
- per-image optional description

Behavior:

- requires at least one uploaded template before images can be submitted
- populates the template dropdown from the current user's templates
- uses one shared template for the current batch
- initializes each selected image from the template tolerance, while allowing per-image overrides
- opens the browser file stream with the configured max size for each image
- resolves the current authenticated user
- calls `InspectionWorkflowService.CreateAsync(...)` sequentially for each image
- continues processing the rest of the batch when one image fails
- displays batch summary plus per-image status messages in the page

### Templates pages

Defined in:

- [UploadTemplate.razor](/Users/delkov/Projects/label-inspection-system/client/Components/Pages/UploadTemplate.razor)
- [Templates.razor](/Users/delkov/Projects/label-inspection-system/client/Components/Pages/Templates.razor)

Routes:

- `/templates/upload`
- `/templates`

Features:

- upload a template image with friendly name, tolerance, and optional description
- paginated list of the current user's uploaded templates
- delete action with confirmation prompt
- deletion blocked when a template is already referenced by one or more inspection images

### Images page

Defined in [Images.razor](/Users/delkov/Projects/label-inspection-system/client/Components/Pages/Images.razor).

Route:

- `/images`

Authorization:

- requires authenticated user

Features:

- paginated list of the current user's uploads
- preview image rendering through generated public URLs
- processing and outcome display
- similarity and tolerance display
- failure message display
- delete action
- re-dispatch action for failed jobs

Current UI gap:

- the `Show defects` action exists in the UI but currently only closes the menu and does not yet render defect visualization

---

## Authentication

Authentication is configured through ASP.NET Core Identity in [Program.cs](/Users/delkov/Projects/label-inspection-system/client/Program.cs) and the components under `Components/Account`.

Key points:

- cookie-based authentication
- Identity user store backed by PostgreSQL
- sign-in requires confirmed account because `RequireConfirmedAccount = true`
- email sending is currently backed by `IdentityNoOpEmailSender`, which means there is no real email delivery implementation in place

Practical consequence:

- account flows exist, but real confirmation email delivery is not configured yet

---

## Endpoints Exposed by the Client

### Blazor/Identity routes

The application exposes standard Blazor routes and Identity account routes.

Examples:

- `/`
- `/images`
- `/images/upload`
- `/templates`
- `/templates/upload`
- `/Account/Login`
- `/Account/Register`

### Public inspection file endpoint

Defined in [InspectionFileEndpoints.cs](/Users/delkov/Projects/label-inspection-system/client/Endpoints/InspectionFileEndpoints.cs).

Pattern:

```text
{PublicFilePathPrefix}/{token}
```

Current effective route:

```text
/inspection-files/{token}
```

### Public template file endpoint

Also defined in [InspectionFileEndpoints.cs](/Users/delkov/Projects/label-inspection-system/client/Endpoints/InspectionFileEndpoints.cs).

Current effective route:

```text
/inspection-template-files/{token}
```

### ML webhook endpoint

Defined in [MlWebhookEndpoints.cs](/Users/delkov/Projects/label-inspection-system/client/Endpoints/MlWebhookEndpoints.cs).

Pattern:

```text
{WebhookPath}
```

Current effective route:

```text
/api/ml/webhook
```

---

## Database Schema Notes

The initial EF Core migration is present under [Data/Migrations](/Users/delkov/Projects/label-inspection-system/client/Data/Migrations).

The schema includes:

- ASP.NET Identity tables
- `InspectionTemplates`
- `InspectionImages`

The `InspectionImages` table stores both workflow state and business result data, which keeps the current implementation simple but means the row acts as both:

- upload metadata record
- processing state record
- final inspection result record

---

## Security and Operational Notes

### Secrets

The current repository still contains environment-specific values in [appsettings.Development.json](/Users/delkov/Projects/label-inspection-system/client/appsettings.Development.json).

Recommended improvement:

- move DB passwords, ML API keys, and webhook secrets to user secrets or environment variables

### Public file serving

Stored files are accessible to anyone with a valid tokenized URL. The token is unguessable, but access is not tied to an authenticated session.

This is necessary for the ML service to fetch the image, but it is still worth recognizing as deliberate public exposure.

### Webhook trust boundary

The webhook endpoint trusts requests with the configured shared secret header. There is no additional signature, timestamp validation, or IP allowlist.

### Development HTTPS

If the ML service must call `https://localhost:7107`, local certificate trust and network reachability need to be solved in the environment where the ML service runs.

This matters especially when:

- the ML service is containerized
- the ML service runs on a different host

---

## Common Development Commands

From the repository root:

Restore tools:

```bash
dotnet tool restore
```

Restore/build the client:

```bash
dotnet build client
```

Run the client:

```bash
dotnet run --project client
```

Apply migrations:

```bash
dotnet ef database update --project client
```

Start local PostgreSQL:

```bash
docker compose up -d postgres
```

Start PostgreSQL plus pgAdmin:

```bash
docker compose --profile tools up -d
```

---

## Known Gaps and Future Improvements

- move secrets out of `appsettings.Development.json`
- add a real email sender for account confirmation and recovery
- implement actual defect visualization for the `Show defects` action
- clarify outcome precedence when both low similarity and defects are returned
- add health checks and/or diagnostics around ML dispatch and webhook processing
- consider background queueing/retry policies if dispatch reliability becomes important
