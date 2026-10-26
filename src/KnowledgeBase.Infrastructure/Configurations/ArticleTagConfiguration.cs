using KnowledgeBase.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeBase.Infrastructure.Configurations;

public class ArticleTagConfiguration : IEntityTypeConfiguration<ArticleTag>
{
    public void Configure(EntityTypeBuilder<ArticleTag> builder)
    {
        builder.ToTable("ArticleTags");

        builder.HasKey(t => new { t.ArticleId, t.Name });

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(t => t.Article)
            .WithMany(a => a.Tags)
            .HasForeignKey(t => t.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Name);
    }
}
