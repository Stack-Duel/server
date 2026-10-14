using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Submissions.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Games;

internal sealed class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.ToTable("game_rounds");

        builder.HasKey(round => round.Id);

        builder.Property(round => round.Id).HasColumnName("id");

        builder.Property<Guid>("game_id").HasColumnName("game_id").IsRequired();

        builder.Property(round => round.ProblemId).HasColumnName("problem_id").IsRequired();

        builder.Property(round => round.Position).HasColumnName("position").IsRequired();

        builder
            .HasOne<Problem>()
            .WithMany()
            .HasForeignKey(round => round.ProblemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex("game_id", nameof(GameRound.Position))
            .IsUnique()
            .HasDatabaseName("IX_game_rounds_game_id_position");

        builder.HasIndex("game_id", nameof(GameRound.ProblemId)).IsUnique();

        builder.HasAlternateKey("Id", "game_id");

        builder.HasAlternateKey("Id", nameof(GameRound.ProblemId), "game_id");

        builder
            .HasMany<GameRoundResult>()
            .WithOne()
            .HasForeignKey(nameof(GameRoundResult.RoundId), "game_id")
            .HasPrincipalKey("Id", "game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany<GameParticipant>()
            .WithOne()
            .HasForeignKey(nameof(GameParticipant.CurrentRoundId), "game_id")
            .HasPrincipalKey("Id", "game_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany<GameRoundSubmission>()
            .WithOne()
            .HasForeignKey(
                "RoundId",
                nameof(GameRoundSubmission.ProblemId),
                nameof(GameRoundSubmission.GameId)
            )
            .HasPrincipalKey("Id", nameof(GameRound.ProblemId), "game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GameRoundResultConfiguration : IEntityTypeConfiguration<GameRoundResult>
{
    public void Configure(EntityTypeBuilder<GameRoundResult> builder)
    {
        builder.ToTable("game_round_results");

        builder.HasKey(result => result.Id);

        builder.Property(result => result.Id).HasColumnName("id");

        builder.Property<Guid>("participant_id").HasColumnName("participant_id").IsRequired();

        builder.Property(result => result.RoundId).HasColumnName("round_id").IsRequired();

        builder.Property<Guid>("game_id").HasColumnName("game_id").IsRequired();

        builder.Property(result => result.Outcome).HasColumnName("outcome").HasConversion<int>().IsRequired();

        builder.Property(result => result.StartedAt).HasColumnName("started_at").IsRequired();

        builder.Property(result => result.CompletedAt).HasColumnName("completed_at").IsRequired();

        builder.HasIndex("participant_id", nameof(GameRoundResult.RoundId)).IsUnique();

        builder.HasAlternateKey(nameof(GameRoundResult.Id), nameof(GameRoundResult.Outcome));

    }
}

internal sealed class GameRoundSolutionConfiguration : IEntityTypeConfiguration<GameRoundSolution>
{
    public void Configure(EntityTypeBuilder<GameRoundSolution> builder)
    {
        builder.ToTable(
            "game_round_solutions",
            table =>
                table.HasCheckConstraint("ck_game_round_solutions_outcome", "outcome = 0")
        );

        builder.HasKey(solution => solution.Id);

        builder.Property(solution => solution.Id).HasColumnName("id");

        builder.Property<Guid>("round_result_id").HasColumnName("round_result_id").IsRequired();

        builder
            .Property<GameRoundOutcome>("outcome")
            .HasColumnName("outcome")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(solution => solution.SubmissionId).HasColumnName("submission_id").IsRequired();

        builder.HasIndex("round_result_id").IsUnique();

        builder
            .HasOne<GameRoundResult>()
            .WithOne(result => result.Solution)
            .HasForeignKey<GameRoundSolution>("round_result_id", "outcome")
            .HasPrincipalKey<GameRoundResult>(result => new { result.Id, result.Outcome })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GameRoundSubmissionConfiguration : IEntityTypeConfiguration<GameRoundSubmission>
{
    public void Configure(EntityTypeBuilder<GameRoundSubmission> builder)
    {
        builder.ToTable("game_round_submissions");

        builder.HasKey(grs => grs.Id);

        builder.Property(grs => grs.Id).HasColumnName("id");

        builder.Property(grs => grs.RoundId).HasColumnName("round_id").IsRequired();

        builder.Property(grs => grs.ParticipantId).HasColumnName("participant_id").IsRequired();

        builder.Property(grs => grs.SetupId).HasColumnName("setup_id").IsRequired();

        builder.Property(grs => grs.SubmissionId).HasColumnName("submission_id").IsRequired();

        builder.Property(grs => grs.ProblemId).HasColumnName("problem_id").IsRequired();

        builder.Property(grs => grs.GameId).HasColumnName("game_id").IsRequired();

        builder.Property(grs => grs.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(grs => grs.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(grs => grs.SubmissionId).IsUnique();

        builder.HasIndex(grs => new { grs.ParticipantId, grs.RoundId, grs.CreatedAt });




        builder
            .HasOne<Submission>()
            .WithMany()
            .HasForeignKey(grs => new { grs.SubmissionId, grs.SetupId, grs.UserId })
            .HasPrincipalKey(submission => new { submission.Id, submission.ProblemSetupId, submission.UserId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}