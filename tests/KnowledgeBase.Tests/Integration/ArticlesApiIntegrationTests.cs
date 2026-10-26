using System.Net;
using System.Text.Json;
using KnowledgeBase.Core.Entities;
using KnowledgeBase.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace KnowledgeBase.Tests.Integration;

public class ArticlesApiIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<KnowledgeBaseDbContext>));
                if (dbDescriptor != null)
                {
                    services.Remove(dbDescriptor);
                }

                services.AddDbContext<KnowledgeBaseDbContext>(options =>
                    options.UseSqlServer(_msSqlContainer.GetConnectionString()));

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
                });
            });
        });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();

        // Apply real migrations to container database
        await db.Database.MigrateAsync();

        // Seed minimal dataset for search scenario
        await SeedSearchDatasetAsync(db);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _msSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task SearchArticles_WithTermAndCategory_ReturnsCaseInsensitivePagedSummaries()
    {
        // Act: mixed-case term "cSHArp_100%" with literal LIKE metacharacters ('_' and '%')
        // should match only tag "csharp_100%" in category "Architecture" (queried as "aRcHiTeCtUrE").
        var response = await _client.GetAsync("/api/articles?search=cSHArp_100%25&category=aRcHiTeCtUrE&page=1&pageSize=10");

        // Assert: 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        // Verify pagination metadata
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(10, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, root.GetProperty("totalPages").GetInt32());

        // Verify matching items (wildcard-like characters '_' and '%' did not match "csharpX1000" or other articles)
        var items = root.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());

        var matchedItem = items[0];
        Assert.Equal("Microservices in Practice", matchedItem.GetProperty("title").GetString());
        Assert.Equal("Architecture", matchedItem.GetProperty("category").GetProperty("name").GetString());

        var tags = matchedItem.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Contains("csharp_100%", tags);

        // Verify summary projection does not expose full article content
        Assert.False(matchedItem.TryGetProperty("content", out _), "Summary projection must not expose full article content.");
    }

    private static async Task SeedSearchDatasetAsync(KnowledgeBaseDbContext db)
    {
        var architectureCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Architecture"
        };

        var devopsCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "DevOps"
        };

        await db.Categories.AddRangeAsync(architectureCategory, devopsCategory);

        var authorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var matchingArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Microservices in Practice",
            Content = "Comprehensive guide to microservices patterns and resilient services.",
            CategoryId = architectureCategory.Id,
            CreatedByUserId = authorId,
            CreatedAtUtc = now.AddDays(-1),
            ViewCount = 42,
            Tags = new List<ArticleTag>
            {
                new() { Name = "patterns" },
                new() { Name = "csharp_100%" },
                new() { Name = "dotnet" }
            }
        };

        var sameCategoryWildcardDecoyArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Cloud Infrastructure Fundamentals",
            Content = "Networking, storage and compute designs in Azure for csharpX1000 workloads.",
            CategoryId = architectureCategory.Id,
            CreatedByUserId = authorId,
            CreatedAtUtc = now.AddDays(-2),
            ViewCount = 15,
            Tags = new List<ArticleTag>
            {
                new() { Name = "azure" },
                new() { Name = "csharpX1000" }
            }
        };

        var differentCategoryMatchingTagArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Automated Pipelines with GitHub Actions",
            Content = "Building and testing csharp_100% applications in CI/CD pipelines.",
            CategoryId = devopsCategory.Id,
            CreatedByUserId = authorId,
            CreatedAtUtc = now.AddDays(-3),
            ViewCount = 20,
            Tags = new List<ArticleTag>
            {
                new() { Name = "devops" },
                new() { Name = "csharp_100%" }
            }
        };

        await db.Articles.AddRangeAsync(
            matchingArticle,
            sameCategoryWildcardDecoyArticle,
            differentCategoryMatchingTagArticle
        );

        await db.SaveChangesAsync();
    }
}
