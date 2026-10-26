namespace KnowledgeBase.Core.Models;

public record ArticleDetail(
    Guid Id,
    string Title,
    string Content,
    CategorySummary? Category,
    IReadOnlyList<string> Tags,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    int ViewCount,
    int UpvoteCount,
    bool UpvotedByCurrentUser
);
