using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Feedback.ValueObjects;
using StackDuel.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Feedback;

internal sealed class FeedbackSubmissionConfiguration : IEntityTypeConfiguration<FeedbackSubmission>
{
    public void Configure(EntityTypeBuilder<FeedbackSubmission> builder)
    {
        builder.ToTable("feedback_submissions");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id).HasColumnName("id");

        builder.Property(f => f.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(f => f.Type).HasColumnName("type").HasConversion<int>().IsRequired();

        builder.Property(f => f.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder
            .Property(f => f.Message)
            .HasColumnName("message")
            .HasMaxLength(FeedbackMessage.MaxLength)
            .HasConversion(m => m.Value, v => new FeedbackMessage(v))
            .IsRequired();

        builder
            .Property(f => f.Rating)
            .HasColumnName("rating")
            .HasConversion(r => r != null ? (int?)r.Value : null, v => v != null ? new FeedbackRating(v.Value) : null);

        builder.Property(f => f.ContextType).HasColumnName("context_type").HasConversion<int>().IsRequired();

        builder.Property(f => f.ContextEntityId).HasColumnName("context_entity_id");

        builder.Property(f => f.AdminNote).HasColumnName("admin_note").HasMaxLength(2000);

        builder.Property(f => f.PageUrl).HasColumnName("page_url").HasMaxLength(2000);

        builder.Property(f => f.UserAgent).HasColumnName("user_agent").HasMaxLength(500);

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<User>().WithMany().HasForeignKey(f => f.UserId).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}