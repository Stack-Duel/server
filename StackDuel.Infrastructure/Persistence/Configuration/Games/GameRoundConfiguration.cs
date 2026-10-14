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

        // The concurrency safety net: when two participants race to be the first to reach a new
        // position, both build the same next-position GameRound in memory, but only one
        // SaveChangesAsync can win this unique index. The loser's save fails cleanly instead of
        // silently persisting two different problems at the same position.
        builder
            .HasIndex("game_id", nameof(GameRound.Position))
            .IsUnique()
            .HasDatabaseName("IX_game_rounds_game_id_position");

        // A problem may not repeat within one game.
        builder.HasIndex("game_id", nameof(GameRound.ProblemId)).IsUnique();

        // Principal side of the composite FKs that game_round_results, game_round_submissions and
        // the participant's current-round cursor use to pin their own game_id back to this round's
        // actual game. Must be declared alternate keys (not merely unique indexes) for Postgres to
        // accept a composite FK referencing them.
        builder.HasAlternateKey("Id", "game_id");

        // Also pins a submission's problem_id to this round's actual problem, together with the
        // game_id above.
        builder.HasAlternateKey("Id", nameof(GameRound.ProblemId), "game_id");

        // Configured here, from the principal side, rather than from GameRoundResultConfiguration:
        // "game_id" is a shadow property on both sides, and HasPrincipalKey(string[]) needs it
        // already declared with its CLR type, which is only guaranteed within this Configure
        // method — the order configurations apply in across entity types is unspecified.
        builder
            .HasMany<GameRoundResult>()
            .WithOne()
            .HasForeignKey(nameof(GameRoundResult.RoundId), "game_id")
            .HasPrincipalKey("Id", "game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Pins a participant's current-round cursor to a round in their own game. Optional, since
        // the cursor is null before their first round and after they finish.
        builder
            .HasMany<GameParticipant>()
            .WithOne()
            .HasForeignKey(nameof(GameParticipant.CurrentRoundId), "game_id")
            .HasPrincipalKey("Id", "game_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Same reasoning: pins problem_id (and game_id) to this round's actual values, for the
        // same shadow-property-ordering reason as above. Also covers what used to be
        // GameRoundSubmissionConfiguration's (RoundId, ProblemId) FK — game_id joined that pairing
        // once GameRoundSubmission pinned its own game_id to the round's.
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

        // A participant finishes each round at most once. The round they are currently on has no row
        // here at all — it lives on game_participants' cursor — which is why every column above is
        // required and this table needs no check constraints reconciling nullable columns.
        builder.HasIndex("participant_id", nameof(GameRoundResult.RoundId)).IsUnique();

        // Redundant on its own (Id is already the key), but it makes this row a legal composite-FK
        // target discriminated by outcome. An outcome-specific table pins its own outcome to a single
        // value and references this key, so such a row can only attach to a result already carrying
        // that outcome — which is what makes two of them mutually exclusive without a trigger. See
        // GameRoundSolutionConfiguration for the worked example.
        builder.HasAlternateKey(nameof(GameRoundResult.Id), nameof(GameRoundResult.Outcome));

        // The Solution relationship is declared from the dependent side, in
        // GameRoundSolutionConfiguration — its FK columns are shadow properties, and declaring the
        // relationship here would make EF infer their types before that Configure method can state
        // them, producing int? shadow props that don't match this (Guid, GameRoundOutcome) key.
    }
}

internal sealed class GameRoundSolutionConfiguration : IEntityTypeConfiguration<GameRoundSolution>
{
    public void Configure(EntityTypeBuilder<GameRoundSolution> builder)
    {
        builder.ToTable(
            "game_round_solutions",
            table =>
                // Pins this row's own discriminator to Solved. Combined with the composite FK into
                // game_round_results(id, outcome), it means a solution can only exist against a
                // result whose outcome IS Solved — so a future outcome-specific table pinned to a
                // different value can never attach to the same result.
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

        // Required, and that is the point of this table existing: a solved round always names the
        // submission that solved it, with no nullable column and no check constraint to enforce it.
        builder.Property(solution => solution.SubmissionId).HasColumnName("submission_id").IsRequired();

        builder.HasIndex("round_result_id").IsUnique();

        // Declared here, from the dependent side, because both FK columns are shadow properties on
        // this entity: they must already carry their CLR types when the relationship is built, which
        // is only guaranteed inside this Configure method.
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

        // Attempt history for one participant on one round, in order — the query that replaces
        // reading solvedProblemSubmissionIds out of the problem_session jsonb. Lookups by round_id
        // alone are already served by the index EF creates for the composite round FK below.
        builder.HasIndex(grs => new { grs.ParticipantId, grs.RoundId, grs.CreatedAt });

        // (i) pins problem_id (and game_id) to the round's actual values — configured from the
        // GameRound side in GameRoundConfiguration, because "game_id" there is a shadow property:
        // HasPrincipalKey(string[]) needs that property already declared with its CLR type, which
        // is only guaranteed within GameRoundConfiguration.Configure itself, not from here where
        // configuration-application order across types is unspecified.

        // (ii) pins problem_id to the setup's actual problem — configured from the ProblemSetup
        // side in ProblemSetupConfiguration, for the same shadow-property-ordering reason as (i).
        // (i) + (ii) together make a mismatched setup unrepresentable: if the two disagree, no
        // value of problem_id satisfies both, so the insert fails.

        // (iii) pins participant_id (and game_id, user_id) to the participant actually playing —
        // configured from the GameParticipant side in GameConfiguration, for the same
        // shadow-property-ordering reason as (i): "game_id" is shadow there too.

        // (iv) pins setup_id (and user_id) to the submission actually used, so a row cannot claim
        // a round/setup pairing the submission never ran, nor a user other than its own.
        builder
            .HasOne<Submission>()
            .WithMany()
            .HasForeignKey(grs => new { grs.SubmissionId, grs.SetupId, grs.UserId })
            .HasPrincipalKey(submission => new { submission.Id, submission.ProblemSetupId, submission.UserId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}