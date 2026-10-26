using KnowledgeBase.Api.Contracts;
using KnowledgeBase.Core.Interfaces;
using KnowledgeBase.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequiredScope("access_as_user")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticlesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateArticleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateArticleCommand(
            request.Title,
            request.Content,
            request.Tags,
            request.CategoryId
        );

        var result = await _articleService.CreateArticleAsync(command, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Data!.Id },
                MapToArticleResponse(result.Data)),
            ServiceResultStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: result.ErrorMessage),
            ServiceResultStatus.ValidationError => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: result.ErrorMessage),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ArticleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _articleService.GetArticleByIdAsync(id, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(MapToArticleResponse(result.Data!)),
            ServiceResultStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: result.ErrorMessage),
            ServiceResultStatus.ValidationError => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: result.ErrorMessage),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ArticleSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ArticleSearchQuery(search, category, page, pageSize);
        var result = await _articleService.SearchArticlesAsync(query, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(new PagedResponse<ArticleSummaryResponse>(
                result.Data!.Items.Select(MapToArticleSummaryResponse).ToList(),
                result.Data.Page,
                result.Data.PageSize,
                result.Data.TotalCount,
                result.Data.TotalPages)),
            ServiceResultStatus.ValidationError => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: result.ErrorMessage),
            ServiceResultStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: result.ErrorMessage),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("{id:guid}/upvote")]
    [ProducesResponseType(typeof(UpvoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upvote(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _articleService.UpvoteArticleAsync(id, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(new UpvoteResponse(
                result.Data!.ArticleId,
                result.Data.UpvoteCount,
                result.Data.UpvotedByCurrentUser)),
            ServiceResultStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: result.ErrorMessage),
            ServiceResultStatus.ValidationError => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: result.ErrorMessage),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static ArticleResponse MapToArticleResponse(ArticleDetail detail) =>
        new(
            detail.Id,
            detail.Title,
            detail.Content,
            detail.Category is not null ? new CategoryResponse(detail.Category.Id, detail.Category.Name) : null,
            detail.Tags,
            detail.CreatedByUserId,
            detail.CreatedAtUtc,
            detail.ViewCount,
            detail.UpvoteCount,
            detail.UpvotedByCurrentUser
        );

    private static ArticleSummaryResponse MapToArticleSummaryResponse(ArticleSummary summary) =>
        new(
            summary.Id,
            summary.Title,
            summary.Category is not null ? new CategoryResponse(summary.Category.Id, summary.Category.Name) : null,
            summary.Tags,
            summary.CreatedByUserId,
            summary.CreatedAtUtc,
            summary.ViewCount,
            summary.UpvoteCount
        );
}
