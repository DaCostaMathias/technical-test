using KnowledgeBase.Core.Entities;
using KnowledgeBase.Core.Interfaces;
using KnowledgeBase.Core.Models;

namespace KnowledgeBase.Core.Services;

public class ArticleService : IArticleService
{
    private const int MaxTitleLength = 200;
    private const int MaxContentLength = 100_000;
    private const int MaxTagCount = 10;
    private const int MaxTagLength = 50;

    private readonly IArticleRepository _repository;
    private readonly IArticleSearchService _searchService;
    private readonly ICurrentUser _currentUser;

    public ArticleService(
        IArticleRepository repository,
        IArticleSearchService searchService,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _searchService = searchService;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<ArticleDetail>> CreateArticleAsync(
        CreateArticleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return ServiceResult<ArticleDetail>.ValidationError("Title is required.");
        }

        if (command.Title.Trim().Length > MaxTitleLength)
        {
            return ServiceResult<ArticleDetail>.ValidationError($"Title cannot exceed {MaxTitleLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Content))
        {
            return ServiceResult<ArticleDetail>.ValidationError("Content is required.");
        }

        if (command.Content.Length > MaxContentLength)
        {
            return ServiceResult<ArticleDetail>.ValidationError($"Content cannot exceed {MaxContentLength} characters.");
        }

        var normalizedTags = new List<string>();
        if (command.Tags is not null)
        {
            var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawTag in command.Tags)
            {
                if (string.IsNullOrWhiteSpace(rawTag))
                {
                    return ServiceResult<ArticleDetail>.ValidationError("Tags cannot be empty or whitespace.");
                }

                var trimmed = rawTag.Trim();
                if (trimmed.Length > MaxTagLength)
                {
                    return ServiceResult<ArticleDetail>.ValidationError($"Tag '{trimmed}' exceeds maximum length of {MaxTagLength} characters.");
                }

                if (seenTags.Add(trimmed))
                {
                    normalizedTags.Add(trimmed);
                }
            }

            if (normalizedTags.Count > MaxTagCount)
            {
                return ServiceResult<ArticleDetail>.ValidationError($"An article cannot have more than {MaxTagCount} tags.");
            }
        }

        if (command.CategoryId.HasValue)
        {
            var categoryExists = await _repository.CategoryExistsAsync(command.CategoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                return ServiceResult<ArticleDetail>.NotFound($"Category with ID '{command.CategoryId.Value}' does not exist.");
            }
        }

        var articleId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var article = new Article
        {
            Id = articleId,
            Title = command.Title.Trim(),
            Content = command.Content,
            CategoryId = command.CategoryId,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = now,
            ViewCount = 0,
            Tags = normalizedTags.Select(tag => new ArticleTag
            {
                ArticleId = articleId,
                Name = tag
            }).ToList()
        };

        await _repository.AddAsync(article, cancellationToken);

        // Fetch full record to ensure any generated/navigation fields are consistent
        var created = await _repository.GetByIdAsync(articleId, cancellationToken) ?? article;

        var detail = new ArticleDetail(
            created.Id,
            created.Title,
            created.Content,
            created.Category is not null ? new CategorySummary(created.Category.Id, created.Category.Name) : null,
            created.Tags.Select(t => t.Name).ToList(),
            created.CreatedByUserId,
            created.CreatedAtUtc,
            created.ViewCount,
            created.Upvotes.Count,
            UpvotedByCurrentUser: false
        );

        return ServiceResult<ArticleDetail>.Success(detail);
    }

    public async Task<ServiceResult<ArticleDetail>> GetArticleByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var article = await _repository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return ServiceResult<ArticleDetail>.NotFound($"Article with ID '{id}' was not found.");
        }

        var incrementedViewCount = await _repository.IncrementViewCountAsync(id, cancellationToken);
        var upvotedByCurrentUser = await _repository.HasUserUpvotedAsync(id, _currentUser.UserId, cancellationToken);

        var detail = new ArticleDetail(
            article.Id,
            article.Title,
            article.Content,
            article.Category is not null ? new CategorySummary(article.Category.Id, article.Category.Name) : null,
            article.Tags.Select(t => t.Name).ToList(),
            article.CreatedByUserId,
            article.CreatedAtUtc,
            incrementedViewCount,
            article.Upvotes.Count,
            upvotedByCurrentUser
        );

        return ServiceResult<ArticleDetail>.Success(detail);
    }

    public async Task<ServiceResult<PagedResult<ArticleSummary>>> SearchArticlesAsync(
        ArticleSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
        {
            return ServiceResult<PagedResult<ArticleSummary>>.ValidationError("Page must be greater than or equal to 1.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            return ServiceResult<PagedResult<ArticleSummary>>.ValidationError("PageSize must be between 1 and 100.");
        }

        var result = await _searchService.SearchAsync(query, cancellationToken);
        return ServiceResult<PagedResult<ArticleSummary>>.Success(result);
    }

    public async Task<ServiceResult<ArticleUpvoteStatus>> UpvoteArticleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var exists = await _repository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            return ServiceResult<ArticleUpvoteStatus>.NotFound($"Article with ID '{id}' was not found.");
        }

        var alreadyUpvoted = await _repository.HasUserUpvotedAsync(id, _currentUser.UserId, cancellationToken);
        if (alreadyUpvoted)
        {
            var existingCount = await _repository.GetUpvoteCountAsync(id, cancellationToken);
            return ServiceResult<ArticleUpvoteStatus>.Success(new ArticleUpvoteStatus(id, existingCount, UpvotedByCurrentUser: true));
        }

        await _repository.AddUpvoteAsync(id, _currentUser.UserId, DateTimeOffset.UtcNow, cancellationToken);
        var currentCount = await _repository.GetUpvoteCountAsync(id, cancellationToken);

        return ServiceResult<ArticleUpvoteStatus>.Success(new ArticleUpvoteStatus(id, currentCount, UpvotedByCurrentUser: true));
    }
}
