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

        // Principal side of the composite FKs that game_round_participants and
        // game_round_submissions use to pin their own game_id back to this round's actual game.
        // Must be declared alternate keys (not merely unique indexes) for Postgres to accept a
        // composite FK referencing them.
        builder.HasAlternateKey("Id", "game_id");

        // Also pins a submission's problem_id to this round's actual problem, together with the
        // game_id above.
        builder.HasAlternateKey("Id", nameof(GameRound.ProblemId), "game_id");

        // Configured here, from the principal side, rather than from GameRoundParticipantConfiguration:
        // "game_id" is a shadow property on both sides, and HasPrincipalKey(string[]) needs it
        // already declared with its CLR type, which is only guaranteed within this Configure
        // method — the order configurations apply in across entity types is unspecified.
        builder
            .HasMany<GameRoundParticipant>()
            .WithOne()
            .HasForeignKey("RoundId", "game_id")
            .HasPrincipalKey("Id", "game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

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

internal sealed class GameRoundParticipantConfiguration : IEntityTypeConfiguration<GameRoundParticipant>
{
    public void Configure(EntityTypeBuilder<GameRoundParticipant> builder)
    {
        builder.ToTable(
            "game_round_participants",
            table =>
            {
                // A round can only be solved by a submission (1 is GameRoundOutcome.Solved).
                // Deliberately NOT an exclusive choice between "skipped" and "has a submission": a
                // participant may fail several attempts and then skip, so a skipped round legitimately
                // carries the last of those submissions.
                table.HasCheckConstraint(
                    "ck_game_round_participants_solved_has_submission",
                    "outcome <> 1 OR submission_id IS NOT NULL"
                );

                // completed_at is set exactly when the round is no longer in progress (0 is
                // GameRoundOutcome.InProgress), so the timestamp and the outcome can never disagree.
                table.HasCheckConstraint(
                    "ck_game_round_participants_completed_at_matches_outcome",
                    "(outcome = 0) = (completed_at IS NULL)"
                );
            }
        );

        builder.HasKey(grp => grp.Id);

        builder.Property(grp => grp.Id).HasColumnName("id");

        builder.Property<Guid>("participant_id").HasColumnName("participant_id").IsRequired();

        builder.Property(grp => grp.RoundId).HasColumnName("round_id").IsRequired();

        builder.Property<Guid>("game_id").HasColumnName("game_id").IsRequired();

        builder.Property(grp => grp.Outcome).HasColumnName("outcome").HasConversion<int>().IsRequired();

        builder.Property(grp => grp.SubmissionId).HasColumnName("submission_id").IsRequired(false);

        builder.Property(grp => grp.StartedAt).HasColumnName("started_at").IsRequired();

        builder.Property(grp => grp.CompletedAt).HasColumnName("completed_at").IsRequired(false);

        // A participant plays each round at most once.
        builder.HasIndex("participant_id", nameof(GameRoundParticipant.RoundId)).IsUnique();

        // Deliberately redundant (Id is already the key) so this row is a legal composite-FK target.
        // An outcome that later needs its own columns — a forfeit reason, solve runtime stats —
        // becomes a new table keyed on (round_participant_id, outcome) with a CHECK pinning its own
        // outcome to one value: a child can then only exist against a row already carrying that
        // outcome, which enforces exclusivity between such tables without a trigger. Adding this key
        // now keeps that extension purely additive instead of a restructure. Outcomes needing no
        // extra columns stay a bare enum value and need nothing here.
        builder.HasAlternateKey(nameof(GameRoundParticipant.Id), nameof(GameRoundParticipant.Outcome));

        // Makes "at most one in-progress round per participant" a database guarantee, not just an
        // in-memory one (see GameParticipant.StartRound/CurrentRound). 0 is GameRoundOutcome.InProgress.
        builder
            .HasIndex("participant_id")
            .IsUnique()
            .HasFilter("outcome = 0")
            .HasDatabaseName("IX_game_round_participants_participant_id_in_progress");

        // The two shadow-property composite FKs pinning this row's participant and round to the
        // same game are declared from the principal side — see GameRoundConfiguration and
        // GameParticipantConfiguration for why.

        // Pins submission_id to the submission actually recorded for this same (participant,
        // round) pairing. Nullable on this side: while submission_id is null, Postgres' default
        // MATCH SIMPLE leaves the constraint unenforced, which is what an in-progress round with
        // no attempt yet needs.
        builder
            .HasOne<GameRoundSubmission>()
            .WithMany()
            .HasForeignKey("SubmissionId", "participant_id", "RoundId")
            .HasPrincipalKey(grs => new { grs.SubmissionId, grs.ParticipantId, grs.RoundId })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
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