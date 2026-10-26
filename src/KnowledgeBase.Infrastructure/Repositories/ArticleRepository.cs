using KnowledgeBase.Core.Entities;
using KnowledgeBase.Core.Interfaces;
using KnowledgeBase.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly KnowledgeBaseDbContext _dbContext;

    public ArticleRepository(KnowledgeBaseDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Article?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Articles
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Tags)
            .Include(a => a.Upvotes)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Articles
            .AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAsync(Article article, CancellationToken cancellationToken = default)
    {
        await _dbContext.Articles.AddAsync(article, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }

    public async Task<int> IncrementViewCountAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        // Atomic database-side update prevents lost updates under concurrent access
        var affected = await _dbContext.Articles
            .Where(a => a.Id == articleId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.ViewCount, a => a.ViewCount + 1),
                cancellationToken);

        if (affected == 0)
        {
            throw new InvalidOperationException($"Cannot increment ViewCount: Article '{articleId}' not found.");
        }

        return await _dbContext.Articles
            .Where(a => a.Id == articleId)
            .Select(a => a.ViewCount)
            .SingleAsync(cancellationToken);
    }

    public async Task<bool> AddUpvoteAsync(
        Guid articleId,
        Guid userId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var upvote = new ArticleUpvote
        {
            ArticleId = articleId,
            UserId = userId,
            CreatedAtUtc = createdAtUtc
        };

        try
        {
            _dbContext.ArticleUpvotes.Add(upvote);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Concurrent duplicate upvote resolved idempotently by database constraint
            _dbContext.Entry(upvote).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<int> GetUpvoteCountAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ArticleUpvotes
            .CountAsync(u => u.ArticleId == articleId, cancellationToken);
    }

    public async Task<bool> HasUserUpvotedAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ArticleUpvotes
            .AnyAsync(u => u.ArticleId == articleId && u.UserId == userId, cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            // 2601: Cannot insert duplicate key row in object with unique index
            // 2627: Violation of %ls constraint
            return sqlEx.Number is 2601 or 2627;
        }

        return false;
    }
}
