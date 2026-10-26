using KnowledgeBase.Core.Models;

namespace KnowledgeBase.Core.Interfaces;

public interface IArticleService
{
    Task<ServiceResult<ArticleDetail>> CreateArticleAsync(CreateArticleCommand command, CancellationToken cancellationToken = default);
    Task<ServiceResult<ArticleDetail>> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<ArticleSummary>>> SearchArticlesAsync(ArticleSearchQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<ArticleUpvoteStatus>> UpvoteArticleAsync(Guid id, CancellationToken cancellationToken = default);
}
