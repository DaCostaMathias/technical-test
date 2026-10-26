namespace KnowledgeBase.Core.Models;

public record CreateArticleCommand(
    string Title,
    string Content,
    IReadOnlyList<string>? Tags,
    Guid? CategoryId
);
