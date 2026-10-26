using System.ComponentModel.DataAnnotations;

namespace KnowledgeBase.Api.Contracts;

public record CreateArticleRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Title,

    [Required]
    [StringLength(100_000, MinimumLength = 1)]
    string Content,

    List<string>? Tags,

    Guid? CategoryId
);
