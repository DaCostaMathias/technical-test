namespace KnowledgeBase.Api.Contracts;

public record UpvoteResponse(
    Guid ArticleId,
    int UpvoteCount,
    bool UpvotedByCurrentUser
);
