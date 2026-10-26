using KnowledgeBase.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeBase.Infrastructure.Configurations;

public class ArticleUpvoteConfiguration : IEntityTypeConfiguration<ArticleUpvote>
{
    public void Configure(EntityTypeBuilder<ArticleUpvote> builder)
    {
        builder.ToTable("ArticleUpvotes");

        // Primary key enforces unique ArticleId + UserId constraint
        builder.HasKey(u => new { u.ArticleId, u.UserId });

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(u => u.Article)
            .WithMany(a => a.Upvotes)
            .HasForeignKey(u => u.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
