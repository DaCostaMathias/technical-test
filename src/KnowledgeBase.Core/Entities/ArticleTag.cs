namespace KnowledgeBase.Core.Entities;

public class ArticleTag
{
    public Guid ArticleId { get; set; }
    public Article? Article { get; set; }

    public string Name { get; set; } = string.Empty;
}
