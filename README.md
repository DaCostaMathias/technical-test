# KnowledgeBase Web API

An ASP.NET Core 10 Web API demonstrating the core backend requirements for the Brownfield Consulting Knowledge Base application.

---

## Architecture & Project Structure

The solution follows Clean Architecture principles, isolating domain logic, persistence, and external concerns:

```text
src/
  KnowledgeBase.Api/             # Web API controllers, DTOs, Entra ID authentication and Problem Details
  KnowledgeBase.Core/            # Domain entities, business rules, services and persistence abstractions
  KnowledgeBase.Infrastructure/  # EF Core DbContext, migrations, SQL Server repositories and search
tests/
  KnowledgeBase.Tests/           # Unit tests and API integration test
```

### Technology Stack & Test Frameworks
- **Framework**: .NET 10 (`v10.0.400` or later) & C# 13
- **Data Persistence**: EF Core 10 & Microsoft SQL Server
- **Authentication**: Microsoft Entra ID (`Microsoft.Identity.Web`) with fallback to local development JWTs (`dotnet user-jwts`)
- **Testing**: xUnit, NSubstitute (unit tests), SQL Server Testcontainers, and `WebApplicationFactory` (integration tests)

---

## Quickstart (Building & Running Tests)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (must be running for integration tests)

### 1. Build and Audit
Restore packages, build without warnings, and check for vulnerable dependencies:

```bash
dotnet restore
dotnet build
dotnet list KnowledgeBase.slnx package --vulnerable --include-transitive
```

> [!NOTE]
> NuGet vulnerability warnings (`NU1901` to `NU1904`) are enforced and unsuppressed. The solution builds with zero warnings and zero package vulnerabilities.

### 2. Run Automated Tests
Execute the full test suite:

```bash
dotnet test
```

The test suite includes:
- **Unit tests**: Business rules covering article service and tag normalization using NSubstitute.
- **Integration test**: End-to-end API verification using `WebApplicationFactory` and an ephemeral SQL Server container orchestrated via **Testcontainers** (migrations are applied automatically).

---

## API Surface

All article endpoints require bearer token authentication with the `access_as_user` delegated scope.

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/articles` | Creates a new article. Author ID is derived strictly from the caller's Entra token. |
| `GET` | `/api/articles/{id}` | Fetches an article by ID. Atomically increments the view count. |
| `GET` | `/api/articles` | Searches articles across title, content, and tags with pagination (`page`, `pageSize`, `search`, `category`). |
| `POST` | `/api/articles/{id}/upvote` | Registers an upvote for the authenticated user. Operation is idempotent. |

### Example Request (`POST /api/articles`)
```http
POST /api/articles
Content-Type: application/json
Authorization: Bearer <ACCESS_TOKEN>

{
  "title": "Client Relationship Management Strategies",
  "content": "<p>Article content goes here.</p>",
  "tags": [ "stakeholders", "communication" ],
  "categoryId": null
}
```

---

## Key Architectural & Design Decisions

### 1. Identity & Dual-Mode Authentication Boundary
- **Caller Identity**: Authorship and upvotes are keyed exclusively to the Entra Object ID (`oid` / `http://schemas.microsoft.com/identity/claims/objectidentifier`) resolved through `ICurrentUser`. Generic subject claims (`sub`, `ClaimTypes.NameIdentifier`) are rejected to prevent identity confusion across tenants.
- **Local Development Mode**: To allow local and offline manual testing without an active Azure tenant, the API supports a development-only JWT mode (`dotnet user-jwts`). This boundary is strictly restricted to `builder.Environment.IsDevelopment() && Configuration["Authentication:UseLocalJwt"] == true`. It never bypasses `[Authorize]`, scope checks, or GUID validation.

### 2. View Counter Concurrency
- `GET /api/articles/{id}` executes an atomic database-level increment using EF Core's `ExecuteUpdateAsync` followed by a scalar query.
- Concurrent requests do not drop increments. While separate update-and-read statements allow simultaneous callers to observe the same final count rather than strict monotonic sequence numbers, this avoids heavy distributed locking for an analytical counter.

### 3. Search and Collation Strategy
- Substring search across title, content, and tag names uses parameterized `EF.Functions.Like`.
- Special wildcard characters (`%`, `_`, `[`, `\`) are strictly escaped to treat input literally and prevent query injection.
- Case-insensitivity relies on SQL Server column collation (`SQL_Latin1_General_CP1_CI_AS`).
- **Scale boundary**: Leading-wildcard matching (`LIKE '%term%'`) is appropriate and proportionate for the exercise dataset (~2,000 articles), but cannot utilize B-tree index seeks. Integration with Azure AI Search and Azure OpenAI is reserved for target hybrid semantic search architectures.

### 4. Tag Deduplication and Consistency
- Application logic normalizes tags by trimming whitespace and filtering duplicates using `StringComparer.OrdinalIgnoreCase`.
- The database enforces uniqueness as the final boundary via a composite primary key (`ArticleId`, `Name`).

### 5. Rich-Text & Category Scope
- Article HTML content is stored as raw string data; HTML sanitization is positioned at the ingestion/rendering boundary.
- Categories are modeled as normalized relational entities. Category management endpoints are separated from article authoring workflows.

---

## Production Considerations

For deployment to cloud environments (e.g., Azure App Service / Container Apps + Azure SQL):
- **Secret Zero**: Replace connection strings and SQL credentials with **Azure Managed Identity** (`DefaultAzureCredential`). Genuine application secrets belong in **Azure Key Vault**.
- **Deterministic Migrations**: Apply EF Core migrations during CD deployment pipelines (`dotnet ef database update` / SQL migration bundles); do not run auto-migrations during API startup.
- **Environment Isolation**: Ensure `Authentication:UseLocalJwt` is strictly disabled; enforce discrete Microsoft Entra App Registrations for Development, Staging, and Production.

---

## Documentation & Guides

For detailed setup instructions, refer to the guides in the [`docs/`](docs/) directory:

- 📖 [**Local Development & Configuration Guide**](docs/local-development.md): Setting up local SQL Server with Docker, managing User Secrets, environment variables, and database migrations.
- 🧪 [**Manual Testing Guide (Without Entra ID)**](docs/manual-testing.md): Complete PowerShell walkthrough for testing all endpoints, token generation, search escaping, and multi-user upvotes using `dotnet user-jwts`.

---

## AI Assistance Disclosure

AI tools were leveraged during this exercise to assist with boilerplate generation, test case expansion, security sanity checking, and documentation refinement. 

All architectural decisions, technology selections, concurrency patterns, security boundaries, and code structures were designed, reviewed, and verified by the candidate.
