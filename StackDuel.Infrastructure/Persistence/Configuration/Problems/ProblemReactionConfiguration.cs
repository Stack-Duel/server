using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class ProblemReactionTypeConfiguration : IEntityTypeConfiguration<ProblemReactionType>
{
    public void Configure(EntityTypeBuilder<ProblemReactionType> builder)
    {
        builder.ToTable("problem_reaction_types");

        builder.HasKey(type => type.Id);

        builder.Property(type => type.Id).HasColumnName("id");

        builder.Property(type => type.Key).HasColumnName("key").HasMaxLength(50).IsRequired();

        builder.HasIndex(type => type.Key).IsUnique();

        builder.Property(type => type.Name).HasColumnName("name").HasMaxLength(100).IsRequired();

        builder.Property(type => type.Emoji).HasColumnName("emoji").HasMaxLength(16).IsRequired(false);

        builder.Property(type => type.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.Property(type => type.IsEnabled).HasColumnName("is_enabled").IsRequired();

        builder.Property(type => type.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}

internal sealed class ProblemReactionConfiguration : IEntityTypeConfiguration<ProblemReaction>
{
    public void Configure(EntityTypeBuilder<ProblemReaction> builder)
    {
        builder.ToTable("problem_reactions");

        builder.HasKey(reaction => reaction.Id);

        builder.Property(reaction => reaction.Id).HasColumnName("id");

        builder.Property(reaction => reaction.ProblemId).HasColumnName("problem_id").IsRequired();

        builder.Property(reaction => reaction.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(reaction => reaction.ReactionTypeId).HasColumnName("reaction_type_id").IsRequired();

        builder.Property(reaction => reaction.IsActive).HasColumnName("is_active").IsRequired();

        builder.Property(reaction => reaction.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(reaction => reaction.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder
            .HasOne<Problem>()
            .WithMany()
            .HasForeignKey(reaction => reaction.ProblemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(reaction => reaction.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<ProblemReactionType>()
            .WithMany()
            .HasForeignKey(reaction => reaction.ReactionTypeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Permanent row per (problem, user, reaction type) — reacting with the same kind twice
        // never inserts a second row, it's found and its IsActive flag flipped instead.
        builder
            .HasIndex(reaction => new
            {
                reaction.ProblemId,
                reaction.UserId,
                reaction.ReactionTypeId,
            })
            .IsUnique();

        // DB-enforced: a user can have at most one active reaction on a given problem at a time,
        // regardless of kind. The handler deactivates any other active row before activating a
        // new one so this is never violated mid-request.
        builder
            .HasIndex(reaction => new { reaction.ProblemId, reaction.UserId })
            .IsUnique()
            .HasFilter("is_active = true")
            .HasDatabaseName("ix_problem_reactions_active_per_user");
    }
}