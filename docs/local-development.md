# Local Development and Configuration Guide

This guide covers setting up a local database, managing configuration and secrets, running database migrations, and configuring authentication for the KnowledgeBase Web API.

For automated test execution and architecture overview, see [README.md](../README.md).  
For step-by-step HTTP API testing, see [Manual Testing Guide](manual-testing.md).

---

## 1. Running SQL Server Locally

If a local SQL Server instance is not already running, you can start one using Docker.

### PowerShell

```powershell
docker run `
  --name knowledgebase-sql `
  -e "ACCEPT_EULA=Y" `
  -e "MSSQL_SA_PASSWORD=<YOUR_LOCAL_SQL_PASSWORD>" `
  -p 1433:1433 `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### Bash

```bash
docker run \
  --name knowledgebase-sql \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=<YOUR_LOCAL_SQL_PASSWORD>" \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

> [!NOTE]
> The SQL Server password must satisfy standard SQL Server complexity requirements.

Verify the container is healthy:

```bash
docker ps
```

---

## 2. Configuration Hierarchy

ASP.NET Core configuration is composed from several sources. Later configuration sources override earlier sources:

1. `appsettings.json`
2. `appsettings.Development.json`
3. .NET User Secrets
4. Environment variables
5. Command-line arguments

Committed configuration files define the required structure and non-sensitive defaults. Passwords, connection strings containing credentials, client secrets, and other sensitive values should not be committed.

### Base Configuration

The committed `src/KnowledgeBase.Api/appsettings.json` contains placeholders rather than real credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Authentication": {
    "UseLocalJwt": false
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "example.onmicrosoft.com",
    "TenantId": "00000000-0000-0000-0000-000000000000",
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "Audience": "api://00000000-0000-0000-0000-000000000000"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- The Entra tenant, client, and audience identifiers represent application metadata rather than credentials. Placeholders are kept committed so the expected configuration shape remains obvious.
- Do not add an Entra client secret to this file; validating incoming bearer tokens does not require a client secret.

### Development Configuration

`src/KnowledgeBase.Api/appsettings.Development.json` contains non-sensitive local overrides:

```json
{
  "Authentication": {
    "UseLocalJwt": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

`Authentication:UseLocalJwt` enables locally issued development JWTs when running in the `Development` environment, allowing manual API testing without requiring a live Microsoft Entra ID tenant.

---

## 3. Local Secrets Management

`src/KnowledgeBase.Api/KnowledgeBase.Api.csproj` includes a committed `<UserSecretsId>`, so `dotnet user-secrets set` and `dotnet user-jwts create` work directly without running `dotnet user-secrets init` or modifying the project file.

### SQL Server Connection String

Store the connection string in User Secrets:

**Bash:**
```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=KnowledgeBaseDb;User Id=sa;Password=<YOUR_LOCAL_SQL_PASSWORD>;TrustServerCertificate=True;" \
  --project src/KnowledgeBase.Api
```

**PowerShell:**
```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=KnowledgeBaseDb;User Id=sa;Password=<YOUR_LOCAL_SQL_PASSWORD>;TrustServerCertificate=True;" --project src/KnowledgeBase.Api
```

To list configured secrets (values will be displayed):

```bash
dotnet user-secrets list --project src/KnowledgeBase.Api
```

### Local Entra Configuration (Optional)

If testing against a live Entra ID tenant locally, you can store your developer-specific tenant information in User Secrets:

```bash
dotnet user-secrets set "AzureAd:Domain" "<YOUR_DOMAIN>.onmicrosoft.com" --project src/KnowledgeBase.Api
dotnet user-secrets set "AzureAd:TenantId" "<YOUR_TENANT_ID>" --project src/KnowledgeBase.Api
dotnet user-secrets set "AzureAd:ClientId" "<YOUR_API_CLIENT_ID>" --project src/KnowledgeBase.Api
dotnet user-secrets set "AzureAd:Audience" "api://<YOUR_API_CLIENT_ID>" --project src/KnowledgeBase.Api
```

These settings are bypassed when `Authentication:UseLocalJwt` is enabled in Development.

---

## 4. Environment Variables Alternative

Configuration can also be supplied via environment variables using double underscores (`__`) as delimiters:

```text
ConnectionStrings__DefaultConnection
Authentication__UseLocalJwt
AzureAd__Domain
AzureAd__TenantId
AzureAd__ClientId
AzureAd__Audience
```

**PowerShell Example:**
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=KnowledgeBaseDb;User Id=sa;Password=<YOUR_LOCAL_SQL_PASSWORD>;TrustServerCertificate=True;"
```

Environment variables take precedence over `appsettings.json`, `appsettings.Development.json`, and User Secrets.

---

## 5. Database Migrations

The API intentionally does **not** automatically apply EF Core migrations during application startup. This avoids silent schema mutations upon service launch and ensures database deployment remains deterministic.

Restore local .NET tools:

```bash
dotnet tool restore
```

Apply pending migrations using the API as the startup project:

**Bash:**
```bash
dotnet ef database update \
  --project src/KnowledgeBase.Infrastructure \
  --startup-project src/KnowledgeBase.Api
```

**PowerShell:**
```powershell
dotnet ef database update --project src/KnowledgeBase.Infrastructure --startup-project src/KnowledgeBase.Api
```

Inspect migration status:

```bash
dotnet ef migrations list \
  --project src/KnowledgeBase.Infrastructure \
  --startup-project src/KnowledgeBase.Api
```

> [!NOTE]
> Categories are not seeded by the migration and there is no category-management endpoint. Set `categoryId` to `null` for manual article creation unless a category row has been inserted directly into SQL.

---

## 6. Authentication Setup

### Production Authentication (Microsoft Entra ID)

In production or deployed environments, the API uses `Microsoft.Identity.Web` to validate Microsoft Entra ID JWT bearer tokens.

The controller enforces:
- `[Authorize]`
- Delegated scope via `[RequiredScope("access_as_user")]`
- User identity mapped strictly through `ICurrentUser` from the Entra `oid` claim (`http://schemas.microsoft.com/identity/claims/objectidentifier`). Generic subject identifiers (`sub`, `ClaimTypes.NameIdentifier`) are explicitly rejected.

### Local Development Authentication (Local JWTs)

When running in `Development` with `Authentication:UseLocalJwt = true`, the API accepts locally issued tokens generated by `dotnet user-jwts`.

The application strictly guards this boundary:

```csharp
var useLocalJwt =
    builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Authentication:UseLocalJwt");

if (useLocalJwt)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
    builder.Services.AddRequiredScopeAuthorization();
}
else
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(
        builder.Configuration);
}
```

Middleware pipeline ordering is preserved:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Local JWT mode enforces:
- `[Authorize]`
- `[RequiredScope("access_as_user")]`
- Valid GUID `oid` resolution through `ICurrentUser` for endpoints that access `ICurrentUser.UserId`

---

## 7. Running the API Locally

`src/KnowledgeBase.Api/Properties/launchSettings.json` defines two launch profiles (both with `ASPNETCORE_ENVIRONMENT=Development`):

- **HTTP profile (`http`)**: `http://localhost:5180`
- **HTTPS profile (`https`)**: `https://localhost:7282` (and `http://localhost:5180`)

When testing over HTTPS, specify the `https` launch profile explicitly:

```bash
dotnet run --project src/KnowledgeBase.Api --launch-profile https
```
