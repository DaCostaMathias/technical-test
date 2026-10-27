# Manual Testing Guide (Without Microsoft Entra ID)

This guide provides an end-to-end walkthrough for manually testing all API endpoints locally using `dotnet user-jwts` and PowerShell, removing any external dependency on Microsoft Entra ID.

For project overview and automated tests, see [README.md](../README.md).  
For Docker and database configuration, see [Local Development Guide](local-development.md).

---

## Prerequisites

1. Ensure SQL Server is running and migrations have been applied (see [Local Development Guide](local-development.md)).
2. Set the environment to `Development`:

**PowerShell:**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

**Bash:**
```bash
export ASPNETCORE_ENVIRONMENT=Development
```

3. Verify `src/KnowledgeBase.Api/appsettings.Development.json` has `UseLocalJwt` set to `true`:

```json
{
  "Authentication": {
    "UseLocalJwt": true
  }
}
```

---

## Step 1: Generate a Local User Token

Generate a test bearer token with the required `access_as_user` scope and a valid GUID `oid` claim:

### PowerShell
```powershell
dotnet user-jwts create `
  --project src/KnowledgeBase.Api `
  --scope access_as_user `
  --claim "scp=access_as_user" `
  --claim "oid=11111111-1111-1111-1111-111111111111" `
  --valid-for 8h
```

### Bash
```bash
dotnet user-jwts create \
  --project src/KnowledgeBase.Api \
  --scope access_as_user \
  --claim "scp=access_as_user" \
  --claim "oid=11111111-1111-1111-1111-111111111111" \
  --valid-for 8h
```

> [!NOTE]
> `dotnet user-jwts --scope` writes the standard `"scope"` claim, whereas `Microsoft.Identity.Web`'s `[RequiredScope]` handler evaluates Entra's `"scp"` (or `"http://schemas.microsoft.com/identity/claims/scope"`) claim. Passing `--claim "scp=access_as_user"` ensures the local token matches the Entra scope claim shape. If your local environment maps object ID claims differently, you can also explicitly pass:
> `--claim "http://schemas.microsoft.com/identity/claims/objectidentifier=11111111-1111-1111-1111-111111111111"`

To inspect or list generated tokens:

```bash
dotnet user-jwts list --project src/KnowledgeBase.Api
dotnet user-jwts print <TOKEN_ID> --project src/KnowledgeBase.Api
```

---

## Step 2: Start the API

Start the Web API from the repository root using the `https` launch profile:

```bash
dotnet run --project src/KnowledgeBase.Api --launch-profile https
```

The `https` profile listens on `https://localhost:7282` (and `http://localhost:5180`). If you run without `--launch-profile https`, the default `http` profile listens only on `http://localhost:5180`.

If your development HTTPS certificate is not trusted:

```powershell
dotnet dev-certs https --trust
```

---

## Step 3: Configure the Test Session

In a PowerShell terminal, initialise your base URL and authentication header:

```powershell
$token = "<PASTE_LOCAL_JWT>"
$baseUrl = "https://localhost:7282"

$headers = @{
  Authorization = "Bearer $token"
}
```

---

## Step 4: Create an Article

Create an article with tags and content. Keep `categoryId` set to `$null` unless a category row has been inserted manually into the `Categories` table (categories are not seeded and there is no category-management endpoint):

```powershell
$body = @{
  title = "Handling difficult stakeholders"
  content = "<p>Start by clarifying concerns and agreeing on outcomes.</p>"
  tags = @(
    "stakeholders",
    "communication"
  )
  categoryId = $null
} | ConvertTo-Json

$article = Invoke-RestMethod `
  -Method Post `
  -Uri "$baseUrl/api/articles" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $body

$article
```

Save the created article ID:

```powershell
$articleId = $article.id
```

**Expected Result:**
- HTTP `201 Created`
- `Location` header pointing to `/api/articles/{id}`
- `createdByUserId` set to `11111111-1111-1111-1111-111111111111`
- `viewCount` = `0`, `upvoteCount` = `0`

---

## Step 5: Retrieve the Article & Verify View Count

Retrieve the article once:

```powershell
$firstRead = Invoke-RestMethod `
  -Method Get `
  -Uri "$baseUrl/api/articles/$articleId" `
  -Headers $headers

$firstRead
```

Retrieve it a second time:

```powershell
$secondRead = Invoke-RestMethod `
  -Method Get `
  -Uri "$baseUrl/api/articles/$articleId" `
  -Headers $headers

$secondRead
```

**Expected Result:**
- Both calls return HTTP `200 OK`
- `viewCount` atomically increments on each read (`1`, then `2`)

---

## Step 6: Search Articles

Search across title, content, and tags:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "$baseUrl/api/articles?search=stakeholder&page=1&pageSize=20" `
  -Headers $headers
```

**Expected Result:**
- Returns paginated article summaries (without full content bodies)
- Escapes special characters (`%`, `_`, `[`) to prevent SQL wildcard injection

---

## Step 7: Upvote the Article (Idempotency Check)

Submit an upvote:

```powershell
$firstUpvote = Invoke-RestMethod `
  -Method Post `
  -Uri "$baseUrl/api/articles/$articleId/upvote" `
  -Headers $headers

$firstUpvote
```

Submit the same upvote again:

```powershell
$secondUpvote = Invoke-RestMethod `
  -Method Post `
  -Uri "$baseUrl/api/articles/$articleId/upvote" `
  -Headers $headers

$secondUpvote
```

**Expected Result:**
- Both calls return HTTP `200 OK`
- `upvotedByCurrentUser` is `true`
- Repeated calls do not duplicate records or increment `upvoteCount` beyond 1

---

## Step 8: Multi-User Upvoting

Generate a token for a second user:

```powershell
dotnet user-jwts create `
  --project src/KnowledgeBase.Api `
  --scope access_as_user `
  --claim "scp=access_as_user" `
  --claim "oid=22222222-2222-2222-2222-222222222222" `
  --valid-for 8h
```

Send an upvote with the second user's credentials:

```powershell
$secondUserHeaders = @{
  Authorization = "Bearer <PASTE_SECOND_LOCAL_JWT>"
}

Invoke-RestMethod `
  -Method Post `
  -Uri "$baseUrl/api/articles/$articleId/upvote" `
  -Headers $secondUserHeaders
```

**Expected Result:**
- HTTP `200 OK`
- Total `upvoteCount` increases to `2`

---

## Step 9: Verify Authentication & Authorisation Boundaries

### Missing Bearer Token
```powershell
Invoke-WebRequest `
  -Method Get `
  -Uri "$baseUrl/api/articles?page=1&pageSize=20" `
  -SkipHttpErrorCheck
```
*Expected: HTTP `401 Unauthorized`.*

### Missing Required Scope (`access_as_user`)
Generate a token without the scope:
```powershell
dotnet user-jwts create `
  --project src/KnowledgeBase.Api `
  --claim "oid=33333333-3333-3333-3333-333333333333" `
  --valid-for 8h
```
Call an endpoint with this token:
*Expected: HTTP `403 Forbidden`.* (Validates that local JWT mode enforces `[RequiredScope("access_as_user")]` via `AddRequiredScopeAuthorization()`).

### Missing Object ID (`oid`)
Generate a token with the `access_as_user` scope but without a valid GUID `oid` claim:
```powershell
dotnet user-jwts create `
  --project src/KnowledgeBase.Api `
  --scope access_as_user `
  --claim "scp=access_as_user" `
  --valid-for 8h
```
Expected prototype behaviour:
- `GET /api/articles` (`Search`) can succeed with HTTP `200 OK` because search does not access `ICurrentUser.UserId`.
- `POST /api/articles` (`Create`), `GET /api/articles/{id}` (`GetById`), and `POST /api/articles/{id}/upvote` (`Upvote`) fail with a generic HTTP `500 Internal Server Error` `ProblemDetails` response when `ICurrentUser.UserId` throws `InvalidOperationException` (the response body does not expose exception details or mention the identity contract).
- **Known prototype limitation**: `GetById` currently increments `ViewCount` in SQL before resolving the current user's upvote state via `ICurrentUser.UserId`, so a request with a missing or malformed `oid` can increment `ViewCount` before failing with HTTP `500`.

---

## Step 10: Clear Local Tokens

When finished, clean up generated development tokens:

```bash
dotnet user-jwts clear --project src/KnowledgeBase.Api
```
