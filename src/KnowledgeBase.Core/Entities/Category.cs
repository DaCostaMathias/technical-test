namespace KnowledgeBase.Core.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
