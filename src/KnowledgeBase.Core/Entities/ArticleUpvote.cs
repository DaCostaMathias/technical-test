namespace KnowledgeBase.Core.Entities;

public class ArticleUpvote
{
    public Guid ArticleId { get; set; }
    public Article? Article { get; set; }

    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
