using KnowledgeBase.Core.Entities;

namespace KnowledgeBase.Core.Interfaces;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Article article, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<int> IncrementViewCountAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<bool> HasUserUpvotedAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> AddUpvoteAsync(Guid articleId, Guid userId, DateTimeOffset createdAtUtc, CancellationToken cancellationToken = default);
    Task<int> GetUpvoteCountAsync(Guid articleId, CancellationToken cancellationToken = default);
}
