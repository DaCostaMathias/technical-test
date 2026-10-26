using KnowledgeBase.Core.Models;

namespace KnowledgeBase.Core.Interfaces;

public interface IArticleSearchService
{
    Task<PagedResult<ArticleSummary>> SearchAsync(ArticleSearchQuery query, CancellationToken cancellationToken = default);
}
