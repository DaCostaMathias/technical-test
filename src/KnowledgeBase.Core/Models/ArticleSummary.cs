namespace KnowledgeBase.Core.Models;

public record ArticleSummary(
    Guid Id,
    string Title,
    CategorySummary? Category,
    IReadOnlyList<string> Tags,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    int ViewCount,
    int UpvoteCount
);
