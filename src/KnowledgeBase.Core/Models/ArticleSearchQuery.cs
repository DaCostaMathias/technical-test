namespace KnowledgeBase.Core.Models;

public record ArticleSearchQuery(
    string? Search,
    string? Category,
    int Page = 1,
    int PageSize = 20
);
