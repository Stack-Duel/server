using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class ProblemPoolConfiguration : IEntityTypeConfiguration<ProblemPool>
{
    public void Configure(EntityTypeBuilder<ProblemPool> builder)
    {
        builder.ToTable("problem_pools");

        builder.HasKey(pool => pool.Id);

        builder.Property(pool => pool.Id).HasColumnName("id");

        builder.Property(pool => pool.Key).HasColumnName("key").HasMaxLength(ProblemPool.MaxKeyLength).IsRequired();

        builder.HasIndex(pool => pool.Key).IsUnique();

        builder.Property(pool => pool.Name).HasColumnName("name").HasMaxLength(ProblemPool.MaxNameLength).IsRequired();

        builder.Property(pool => pool.Description).HasColumnName("description").IsRequired(false);

        builder.Property(pool => pool.CreatedAt).HasColumnName("created_at").IsRequired();

        builder
            .HasMany<ProblemPoolItem>("_items")
            .WithOne()
            .HasForeignKey("pool_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(pool => pool.ProblemIds);
    }
}

internal sealed class ProblemPoolItemConfiguration : IEntityTypeConfiguration<ProblemPoolItem>
{
    public void Configure(EntityTypeBuilder<ProblemPoolItem> builder)
    {
        builder.ToTable("problem_pool_problems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");

        builder.Property<Guid>("pool_id").HasColumnName("pool_id").IsRequired();

        builder.Property(item => item.ProblemId).HasColumnName("problem_id").IsRequired();

        builder.Property(item => item.Position).HasColumnName("position").IsRequired();

        builder
            .Property(item => item.AddedAt)
            .HasColumnName("added_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder
            .HasOne<Problem>()
            .WithMany()
            .HasForeignKey(item => item.ProblemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("pool_id", nameof(ProblemPoolItem.ProblemId)).IsUnique();
    }
}