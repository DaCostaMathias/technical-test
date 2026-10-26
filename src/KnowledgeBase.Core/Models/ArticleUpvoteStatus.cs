namespace KnowledgeBase.Core.Models;

public record ArticleUpvoteStatus(
    Guid ArticleId,
    int UpvoteCount,
    bool UpvotedByCurrentUser
);
