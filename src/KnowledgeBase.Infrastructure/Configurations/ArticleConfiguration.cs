using KnowledgeBase.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeBase.Infrastructure.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Content)
            .IsRequired()
            .HasMaxLength(100_000);

        builder.Property(a => a.CreatedByUserId)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.Property(a => a.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Articles)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.CreatedAtUtc);
        builder.HasIndex(a => a.CategoryId);
    }
}
