namespace KnowledgeBase.Api.Contracts;

public record ArticleResponse(
    Guid Id,
    string Title,
    string Content,
    CategoryResponse? Category,
    IReadOnlyList<string> Tags,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    int ViewCount,
    int UpvoteCount,
    bool UpvotedByCurrentUser
);
