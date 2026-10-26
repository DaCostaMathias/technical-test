namespace KnowledgeBase.Core.Entities;

public class Article
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public int ViewCount { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<ArticleTag> Tags { get; set; } = new List<ArticleTag>();
    public ICollection<ArticleUpvote> Upvotes { get; set; } = new List<ArticleUpvote>();
}
