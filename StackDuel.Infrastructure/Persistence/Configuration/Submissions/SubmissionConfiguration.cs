using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.ValueObjects;
using StackDuel.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace StackDuel.Infrastructure.Persistence.Configuration.Submissions;

internal sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(s => s.ProblemSetupId).HasColumnName("problem_setup_id").IsRequired();

        builder.Property(s => s.GameId).HasColumnName("game_id").IsRequired(false);

        builder.Property(s => s.Type).HasColumnName("type").HasConversion<int>().IsRequired();

        builder.Property(s => s.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();

        builder
            .Property(s => s.SourceCode)
            .HasConversion(v => v.Value, v => new SourceCode(v))
            .HasColumnName("source_code")
            .HasMaxLength(SourceCode.MaxLength)
            .IsRequired();

        builder
            .Property(s => s.AdditionalFiles)
            .HasColumnName("additional_files")
            .HasConversion(
                files => JsonSerializer.Serialize(files, (JsonSerializerOptions?)null),
                json =>
                    JsonSerializer.Deserialize<IReadOnlyList<SubmissionSourceFile>>(json, (JsonSerializerOptions?)null)
                    ?? new List<SubmissionSourceFile>()
            )
            .IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).IsRequired().OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<ProblemSetup>()
            .WithMany()
            .HasForeignKey(s => s.ProblemSetupId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Game>()
            .WithMany()
            .HasForeignKey(s => s.GameId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(s => s.Results)
            .WithOne()
            .HasForeignKey("submission_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Results).HasField("_results").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}