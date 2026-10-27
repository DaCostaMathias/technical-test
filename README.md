# Brownfield Consulting Knowledge Base

This repository contains the submission for the Brownfield Consulting Knowledge Base technical exercise. It includes the architecture proposal, a working ASP.NET Core API slice with SQL Server persistence, focused automated tests, a customer-facing AI search recommendation, and the legacy-document migration strategy bonus.

Note on customer naming: the supplied brief refers to Brownfield Consulting in the main scenario and once to Greenfield Consulting in the technical recommendation prompt. Brownfield Consulting is treated as the intended customer name throughout this submission because it is used in the main scenario.

## Submission Guide

- [Architecture proposal](architecture/design.md): Defines the proposed four-week minimum viable product (MVP) architecture on Azure, covering component choices, trade-offs, hybrid AI search, category suggestions, the data model, operational resilience, and MVP scoping.
- [AI-powered search recommendation](recommendation/ai-search-recommendation.md): Presents a plain-language recommendation for Brownfield Consulting's CTO explaining why hybrid search in Azure AI Search is recommended, what it does and does not do well, cost drivers, and rollout steps.
- [Working API](src/): Contains the buildable .NET 10 ASP.NET Core Web API separated into `KnowledgeBase.Api`, `KnowledgeBase.Core`, and `KnowledgeBase.Infrastructure` projects with Entity Framework Core 10 and SQL Server persistence.
- [Automated tests](tests/KnowledgeBase.Tests/): Contains two unit tests covering core business rules with xUnit and NSubstitute and one API integration test using `WebApplicationFactory` and SQL Server Testcontainers.
- [Migration strategy bonus](architecture/design.md#12-migration-strategy-for-existing-documents): Outlines a twelve-step, semi-automated batch pipeline for inventorying, extracting, categorising, reviewing, and indexing the 2,000 legacy Word and PDF documents from the shared drive.
- [Local development guide](docs/local-development.md): Explains how to run SQL Server in Docker, configure .NET User Secrets and environment variables, apply Entity Framework Core migrations, and configure local or Entra authentication.
- [Manual testing guide](docs/manual-testing.md): Provides a step-by-step PowerShell walkthrough for generating local bearer tokens with `dotnet user-jwts` and verifying every API endpoint and authorisation boundary without an active Microsoft Entra tenant.
- [Supplied exercise brief](technical-exercise-TechLeadOctobre2026.pdf): Retains the original take-home exercise instructions and evaluation criteria in the repository root for reviewer convenience.

## Implementation Summary

The implemented API in [`src/`](src/) is a focused exercise slice built to demonstrate solution structure, C# code quality, data access patterns, validation, error handling, and testability. It is not the entire proposed MVP.

The working API currently includes:

- **.NET 10 and ASP.NET Core**: A controller-based Web API targeting `net10.0` with nullable reference types enabled across all projects.
- **API / Core / Infrastructure separation**: `KnowledgeBase.Api` hosts controllers, contracts, authentication wiring, and identity resolution; `KnowledgeBase.Core` holds domain entities, interfaces, models, and `ArticleService`; `KnowledgeBase.Infrastructure` implements `KnowledgeBaseDbContext`, migrations, `ArticleRepository`, and `EfArticleSearchService`.
- **Entity Framework Core with SQL Server**: Relational mapping and an initial schema migration (`20261026210517_InitialCreate`) for `Articles`, `Categories`, `ArticleTags`, and `ArticleUpvotes`.
- **Authentication and scope skeleton**: Configured with `Microsoft.Identity.Web` by default, enforcing `[Authorize]` and `[RequiredScope("access_as_user")]` on `ArticlesController`.
- **Strict Entra object ID resolution**: Caller identity is resolved through `ICurrentUser` (`CurrentUser`) exclusively from a valid GUID Microsoft Entra `oid` claim, rejecting generic subject claims.
- **Development-only local JWT support**: Allows offline manual testing in `Development` when `Authentication:UseLocalJwt` is enabled, while preserving `[Authorize]`, `[RequiredScope("access_as_user")]`, and `ICurrentUser` GUID `oid` enforcement.
- **Article creation and retrieval**: Validated article creation and retrieval by ID with synchronous atomic view-count increments (`ExecuteUpdateAsync`).
- **SQL-backed search, filtering, and pagination**: Substring search across article title, content, and tag names using escaped `EF.Functions.Like` patterns, category-name filtering, and pagination.
- **Idempotent per-user upvotes**: Single upvote per authenticated user per article, enforced by application checks and a composite primary key `(ArticleId, UserId)`.
- **Structured Problem Details responses**: RFC 7807 `ProblemDetails` payloads for validation failures (`400 Bad Request`), missing resources (`404 Not Found`), and unhandled errors (`500 Internal Server Error`).
- **Focused automated test suite**: Two xUnit and NSubstitute unit tests in [`ArticleServiceTests.cs`](tests/KnowledgeBase.Tests/Unit/ArticleServiceTests.cs) and one `WebApplicationFactory` and SQL Server Testcontainers integration test in [`ArticlesApiIntegrationTests.cs`](tests/KnowledgeBase.Tests/Integration/ArticlesApiIntegrationTests.cs).

## Implemented API Surface

All endpoints are rooted at `/api/articles` in [`ArticlesController.cs`](src/KnowledgeBase.Api/Controllers/ArticlesController.cs) and require a bearer token with the `access_as_user` scope. Validation and missing-resource responses use Problem Details.

- **`POST /api/articles`**: Creates an article from `title`, `content`, optional `tags`, and optional `categoryId`, deriving authorship from the authenticated caller and returning `201 Created`.
- **`GET /api/articles/{id}`**: Returns the full article content and synchronously increments `ViewCount` when the article exists, returning `200 OK` or `404 Not Found`.
- **`GET /api/articles`**: Searches and filters articles using the `search` and `category` query parameters with `page` and `pageSize` pagination, returning `200 OK` with paged summaries that omit full `content`.
- **`POST /api/articles/{id}/upvote`**: Registers an idempotent upvote for the authenticated caller on the specified article, returning `200 OK` for first and repeated successful calls, or `404 Not Found` when the article does not exist.

Verified behavioural details and input limits:

- **Input limits**:
  - `title`: 1 to 200 characters (trimmed before persistence).
  - `content`: 1 to 100,000 characters.
  - `tags`: maximum 10 distinct tags after case-insensitive de-duplication; maximum 50 characters per tag; empty or whitespace-only tags are rejected.
- **Category handling**:
  - On `POST /api/articles`, `categoryId` must refer to an existing category row when supplied, otherwise returning `404 Not Found`.
  - No categories are seeded by the migration and no category-management endpoint exists; use `categoryId: null` for manual creation unless a category row has been inserted into SQL Server.
  - On `GET /api/articles`, `category` represents the category name (`Category.Name`) matched via SQL Server's case-insensitive collation.
- **Pagination**:
  - `page` defaults to `1` (minimum `1`).
  - `pageSize` defaults to `20` and is capped at `100` (`1` to `100`).

## Implemented vs Proposed Scope

### Implemented in This Repository

The code under [`src/`](src/) and [`tests/`](tests/) implements only:

- Working controller-based API (`ArticlesController`).
- SQL Server persistence (`KnowledgeBaseDbContext`, `ArticleRepository`) and initial migration.
- Ordinary EF-backed search (`EfArticleSearchService`).
- Authentication and scope skeleton (`Microsoft.Identity.Web`, `[Authorize]`, `[RequiredScope("access_as_user")]`, and `ICurrentUser`).
- Local development JWT mode (`Authentication:UseLocalJwt`).
- View counts (`ExecuteUpdateAsync`).
- Idempotent upvotes.
- Validation and structured `ProblemDetails` errors.
- Focused unit and integration tests.

### Proposed in the MVP Architecture

The target design in [`architecture/design.md`](architecture/design.md) defines the full four-week MVP for Brownfield Consulting, including:

- React and TypeScript UI on Azure Static Web Apps.
- API hosting on Azure App Service and canonical persistence in Azure SQL Database.
- Attachment storage in Azure Blob Storage.
- Article edit (`PUT`) and administrative deletion (`DELETE`) workflows.
- `ArticleVersion` history, `ArticleView` logs, and `AuditEvent` records.
- Azure AI Search hybrid retrieval.
- Azure OpenAI embeddings for query and chunk vectorisation.
- Author-confirmed category suggestions via Azure OpenAI structured outputs.
- Transactional SQL outbox (`SearchOutboxEntry`) and Azure Function indexing.
- Legacy document migration tooling.
- Application Insights, Azure Key Vault, and Azure Managed Identities.

These architecture capabilities are not implemented in the working API repository. No Azure AI Search, Azure OpenAI, React, Azure resources, or migration tooling exist in code.

## Key Decisions

- **Restrained API / Core / Infrastructure structure**: Isolates HTTP concerns, business rules, and EF Core SQL Server persistence cleanly without over-engineering for 200 consultants.
- **Thin controllers and application services**: `ArticlesController` maps HTTP contracts and status codes while `ArticleService` coordinates validation, tag normalisation, and persistence via `ServiceResult<T>`.
- **No unnecessary abstractions**: MediatR, AutoMapper, generic repositories, and unused AI abstractions were omitted to keep the solution direct and maintainable.
- **SQL Server Testcontainers over EF InMemory or SQLite**: Integration testing runs against a real SQL Server container so migrations, constraints, collations, and escaped `LIKE` predicates are verified accurately.
- **`IArticleSearchService` as the search replacement boundary**: Isolating search behind [`IArticleSearchService`](src/KnowledgeBase.Core/Interfaces/IArticleSearchService.cs) allows `EfArticleSearchService` to serve the prototype today and act as a degraded fallback when Azure AI Search replaces it in the MVP.
- **Authoritative SQL and rebuildable derived index**: In the proposed architecture, Azure SQL Database remains authoritative while Azure AI Search is a rebuildable derived index fed asynchronously through a SQL outbox.
- **Hybrid retrieval over vector-only search**: Combining keyword and vector retrieval handles both exact consulting acronyms/identifiers and conceptual queries across differing phrasing.
- **Confirmed category suggestions and independent publishing**: Category suggestions require author confirmation, and article publishing commits to SQL independently of asynchronous AI indexing.
- **Migration strategy selected as bonus**: Addressing the 2,000 existing Word and PDF documents in [Section 12 of `architecture/design.md`](architecture/design.md#12-migration-strategy-for-existing-documents) targets a primary adoption risk for Brownfield Consulting.

## Assumptions and Clarifying Questions

### Verified Assumptions

- Brownfield Consulting is treated as the intended customer name across the submission.
- An article has zero or one category (`CategoryId` is nullable), while multiple free-form labels are modelled as tags.
- Each authenticated user (`oid`) can upvote an article once, and repeat upvotes are idempotent.
- Each successful `GET /api/articles/{id}` counts as one view request, not one unique viewer.
- Tags are trimmed and de-duplicated case-insensitively (`StringComparer.OrdinalIgnoreCase`).
- Rich-text content is stored but not sanitised or rendered in this API phase.
- All authenticated consultants share read access in the proposed MVP.

### Clarifying Questions for the Customer

1. Can the internal maintenance team support React and TypeScript alongside .NET, or should Blazor be reconsidered?
2. Which Azure region, Azure OpenAI deployments, and quotas are available?
3. How many existing PDFs are scanned images requiring OCR?
4. Does any content require per-document access restrictions?
5. Are public Azure endpoints protected by Entra ID and TLS acceptable for the MVP, or are private endpoints required?

## Validation Status

The following results were observed at the time of submission:

- `dotnet build` completed with zero warnings and zero errors.
- Exactly three tests passed, zero failed, and zero were skipped.
- The package vulnerability audit reported no known vulnerable packages from configured sources.
- The integration test requires Docker because it runs SQL Server Testcontainers.

These observations reflect verification at submission time rather than a permanent guarantee.

Verification commands:

```bash
dotnet tool restore
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet list KnowledgeBase.slnx package --vulnerable --include-transitive
```

## Quick Start

### Prerequisites

- .NET 10 SDK
- Docker Desktop or compatible container runtime (Docker must be running for the integration test)

### Build and Test

```bash
dotnet tool restore
dotnet restore
dotnet build
dotnet test
```

### Detailed Setup and Manual Testing

- See [`docs/local-development.md`](docs/local-development.md) for SQL Server, User Secrets, migrations, Entra, and Development local JWT setup.
- See [`docs/manual-testing.md`](docs/manual-testing.md) for bearer-token generation and endpoint verification.

## Local Authentication Modes

[`Program.cs`](src/KnowledgeBase.Api/Program.cs) configures one of two authentication modes:

- **Microsoft Entra ID**:
  - Default outside the Development local-JWT branch.
  - Configured through `Microsoft.Identity.Web` (`AddMicrosoftIdentityWebApiAuthentication`).
  - Requires a valid token with `access_as_user` and a GUID `oid`.
- **Development Local JWT**:
  - Active only when environment is `Development` and `Authentication:UseLocalJwt` is `true`.
  - Uses `JwtBearer` authentication and registers `AddRequiredScopeAuthorization()`.
  - Enforces `[Authorize]`, enforces `[RequiredScope("access_as_user")]`, and requires a valid GUID `oid` on endpoints using `ICurrentUser`.
  - Signing keys and tokens are stored outside the repository through User Secrets and `dotnet user-jwts`.
  - Intended only for local manual testing.

See [`docs/local-development.md`](docs/local-development.md) and [`docs/manual-testing.md`](docs/manual-testing.md) for setup details.

## Known Prototype Limitations

- **No category seed data or category CRUD endpoint**: `categoryId` must match a row in `Categories`, which is empty unless seeded manually in SQL (see [`docs/local-development.md`](docs/local-development.md)).
- **Rich-text HTML stored without server-side sanitisation**: `content` is length-validated and stored raw; allowlist sanitisation belongs in the full MVP.
- **SQL substring search limitations**: `EfArticleSearchService` uses leading-wildcard `LIKE '%term%'` predicates, which are not semantic search and are not index-seekable.
- **Lazy `oid` resolution via `ICurrentUser`**: Missing or malformed `oid` is resolved only when `ICurrentUser` is accessed:
  - `Search` (`GET /api/articles`) can succeed without `oid`.
  - `Create`, `GetById`, and `Upvote` return generic `500` Problem Details.
  - `GetById` currently increments `ViewCount` before failing when `oid` is invalid (see [`docs/manual-testing.md`](docs/manual-testing.md)).
- **`ViewCount` semantics**: `ViewCount` is an atomic request counter (`ExecuteUpdateAsync`), not unique-user analytics, and concurrent readers may receive the same later persisted count.
- **Tag de-duplication vs SQL collation**: Application tag de-duplication (`StringComparer.OrdinalIgnoreCase`) and SQL Server collation (`SQL_Latin1_General_CP1_CI_AS`) can differ for rare Unicode cases.

## AI Assistance Disclosure

AI tools used during this submission:

- **Microsoft Copilot**: Used for planning, architectural trade-off exploration, decision support, and prompt development.
- **Google Antigravity with Gemini 4 Argon**: Used for implementation scaffolding, repository review, targeted corrections, documentation generation and refinement, and command and behaviour verification.

Candidate ownership and verification:

- The candidate selected the architecture and technology choices, and set the business rules and scope boundaries.
- The candidate reviewed all generated code and documentation, ran build, test, migration, and package-audit checks, and corrected findings before inclusion.
- The candidate remains responsible for the submitted result.

## If More Time Were Available

With additional time, the following items would be prioritised:

1. Add API integration tests for create, retrieve, first-time/concurrent upvotes, and `401`/`403` boundaries.
2. Enforce valid `oid` through an authorisation policy before controller/service execution, avoiding current `500` behaviour and the failed-request view increment.
3. Add category seeding or a category read endpoint.
4. Add allowlist-based rich-text HTML sanitisation.
5. Implement update, administrative deletion, article versions, and audit events.
6. Run a search relevance spike using representative Brownfield Consulting content and queries.
7. Validate migration extraction quality against a representative sample.
8. Add infrastructure as code and CI/CD once deployment and networking requirements are confirmed.

## Developer Notes

- Database migrations are explicit (`dotnet ef database update`) and are not applied on API startup.
- Search case-insensitivity depends on SQL Server collation (`SQL_Latin1_General_CP1_CI_AS`), and `LIKE` metacharacters (`\`, `%`, `_`, `[`) are escaped.
- View increments use `ExecuteUpdateAsync`, and upvote uniqueness is enforced by the composite database key `(ArticleId, UserId)`.
- No secrets are committed (`UserSecretsId` is configured in `KnowledgeBase.Api.csproj`), and `AzureAd` values in `appsettings.json` are placeholders.

See [`docs/local-development.md`](docs/local-development.md) and [`docs/manual-testing.md`](docs/manual-testing.md) for full configuration and testing details.

## Supplied Brief and Bonus

- The supplied exercise PDF is retained at [`technical-exercise-TechLeadOctobre2026.pdf`](technical-exercise-TechLeadOctobre2026.pdf) for reviewer convenience.
- The migration bonus is included in [Section 12 of `architecture/design.md`](architecture/design.md#12-migration-strategy-for-existing-documents).
