using KnowledgeBase.Core.Interfaces;
using KnowledgeBase.Core.Models;
using KnowledgeBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Services;

public class EfArticleSearchService : IArticleSearchService
{
    private const string LikeEscapeCharacter = @"\";

    private readonly KnowledgeBaseDbContext _dbContext;

    public EfArticleSearchService(KnowledgeBaseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ArticleSummary>> SearchAsync(
        ArticleSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.Articles
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categoryName = query.Category.Trim();
            // Case-insensitive matching relies on the configured SQL Server case-insensitive collation.
            queryable = queryable.Where(a => a.Category != null && a.Category.Name == categoryName);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var escapedTerm = EscapeLikePattern(query.Search.Trim());
            var pattern = $"%{escapedTerm}%";

            queryable = queryable.Where(a =>
                EF.Functions.Like(a.Title, pattern, LikeEscapeCharacter) ||
                EF.Functions.Like(a.Content, pattern, LikeEscapeCharacter) ||
                a.Tags.Any(t => EF.Functions.Like(t.Name, pattern, LikeEscapeCharacter)));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / query.PageSize);

        var items = await queryable
            .OrderByDescending(a => a.CreatedAtUtc)
            .ThenBy(a => a.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new ArticleSummary(
                a.Id,
                a.Title,
                a.Category != null ? new CategorySummary(a.Category.Id, a.Category.Name) : null,
                a.Tags.Select(t => t.Name).ToList(),
                a.CreatedByUserId,
                a.CreatedAtUtc,
                a.ViewCount,
                a.Upvotes.Count
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<ArticleSummary>(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages
        );
    }

    private static string EscapeLikePattern(string input) =>
        input
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
}
