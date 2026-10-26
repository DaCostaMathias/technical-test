namespace KnowledgeBase.Api.Contracts;

public record ArticleSummaryResponse(
    Guid Id,
    string Title,
    CategoryResponse? Category,
    IReadOnlyList<string> Tags,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    int ViewCount,
    int UpvoteCount
);
