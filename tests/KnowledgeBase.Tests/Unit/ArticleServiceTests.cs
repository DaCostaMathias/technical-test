using KnowledgeBase.Core.Entities;
using KnowledgeBase.Core.Interfaces;
using KnowledgeBase.Core.Models;
using KnowledgeBase.Core.Services;
using NSubstitute;
using Xunit;

namespace KnowledgeBase.Tests.Unit;

public class ArticleServiceTests
{
    private readonly IArticleRepository _repository = Substitute.For<IArticleRepository>();
    private readonly IArticleSearchService _searchService = Substitute.For<IArticleSearchService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ArticleService _sut;

    public ArticleServiceTests()
    {
        _sut = new ArticleService(_repository, _searchService, _currentUser);
    }

    [Fact]
    public async Task UpvoteArticleAsync_WhenAlreadyUpvoted_IsIdempotentAndDoesNotInsertNewUpvote()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const int existingUpvoteCount = 7;

        _currentUser.UserId.Returns(userId);
        _repository.ExistsAsync(articleId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.HasUserUpvotedAsync(articleId, userId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetUpvoteCountAsync(articleId, Arg.Any<CancellationToken>()).Returns(existingUpvoteCount);

        // Act
        var result = await _sut.UpvoteArticleAsync(articleId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(articleId, result.Data.ArticleId);
        Assert.Equal(existingUpvoteCount, result.Data.UpvoteCount);
        Assert.True(result.Data.UpvotedByCurrentUser);

        await _repository.DidNotReceiveWithAnyArgs().AddUpvoteAsync(
            default,
            default,
            default,
            default);
    }

    [Fact]
    public async Task GetArticleByIdAsync_WhenArticleExists_IncrementsViewCountSynchronouslyAndReturnsIncrementedValue()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        const int initialViewCount = 10;
        const int incrementedViewCount = 11;

        var article = new Article
        {
            Id = articleId,
            Title = "Scalable .NET Architectures",
            Content = "Detailed architecture content...",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2),
            ViewCount = initialViewCount
        };

        _currentUser.UserId.Returns(currentUserId);
        _repository.GetByIdAsync(articleId, Arg.Any<CancellationToken>()).Returns(article);
        _repository.IncrementViewCountAsync(articleId, Arg.Any<CancellationToken>()).Returns(incrementedViewCount);
        _repository.HasUserUpvotedAsync(articleId, currentUserId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.GetArticleByIdAsync(articleId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(articleId, result.Data.Id);
        Assert.Equal(incrementedViewCount, result.Data.ViewCount);
        await _repository.Received(1).IncrementViewCountAsync(articleId, Arg.Any<CancellationToken>());

        // Verify missing article branch does not request increment
        var missingArticleId = Guid.NewGuid();
        _repository.GetByIdAsync(missingArticleId, Arg.Any<CancellationToken>()).Returns((Article?)null);

        var missingResult = await _sut.GetArticleByIdAsync(missingArticleId);

        Assert.Equal(ServiceResultStatus.NotFound, missingResult.Status);
        await _repository.DidNotReceive().IncrementViewCountAsync(missingArticleId, Arg.Any<CancellationToken>());
    }
}
