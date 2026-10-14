using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Domain.Tests.Games.Entities;

public class GameParticipantTests
{
    private static readonly Guid GameModeId = Guid.NewGuid();
    private static readonly Guid PoolId = Guid.NewGuid();
    private static readonly Guid TrackId = Guid.NewGuid();

    private static (Game Game, Guid UserId, Guid ProblemId) CreateRunningGameWithActiveRound()
    {
        Guid userId = Guid.NewGuid();
        Game game = new(GameModeId, PoolId, [TrackId], [userId, Guid.NewGuid()], timeLimitInSeconds: 300);
        game.Start();

        Guid problemId = Guid.NewGuid();
        game.AppendRound(problemId);
        game.StartRoundFor(userId, problemId);

        return (game, userId, problemId);
    }

    private static GameRoundResult RoundResultFor(Game game, GameParticipant participant, Guid problemId)
    {
        Guid roundId = game.Rounds.Single(round => round.ProblemId == problemId).Id;
        return participant.RoundResults.Single(result => result.RoundId == roundId);
    }

    private static Guid AppendNextRound(Game game)
    {
        Guid problemId = Guid.NewGuid();
        game.AppendRound(problemId);
        return problemId;
    }

    [Fact]
    public void Constructor_StartsWithTotalSkipsRemaining()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.Equal(GameParticipant.TotalSkips, participant.SkipsRemaining);
    }

    [Fact]
    public void SkipCurrentRoundAndAdvance_DecrementsSkipsRemaining()
    {
        var (game, userId, _) = CreateRunningGameWithActiveRound();
        GameParticipant participant = game.Participants.Single(p => p.UserId == userId);

        game.SkipCurrentRoundAndAdvance(userId, AppendNextRound(game));

        Assert.Equal(GameParticipant.TotalSkips - 1, participant.SkipsRemaining);
    }

    [Fact]
    public void SkipCurrentRoundAndAdvance_MarksTheRoundSkipped_NotSolved()
    {
        var (game, userId, problemId) = CreateRunningGameWithActiveRound();
        GameParticipant participant = game.Participants.Single(p => p.UserId == userId);

        game.SkipCurrentRoundAndAdvance(userId, AppendNextRound(game));

        GameRoundResult progress = RoundResultFor(game, participant, problemId);
        Assert.Equal(GameRoundOutcome.Skipped, progress.Outcome);
    }

    [Fact]
    public void SkipCurrentRoundAndAdvance_SetsCurrentProblemToTheNewProblem()
    {
        var (game, userId, _) = CreateRunningGameWithActiveRound();
        Guid nextProblemId = AppendNextRound(game);

        game.SkipCurrentRoundAndAdvance(userId, nextProblemId);

        Assert.Equal(nextProblemId, game.CurrentProblemIdFor(userId));
    }

    [Fact]
    public void SkipCurrentRoundAndAdvance_DoesNotAffectScore()
    {
        var (game, userId, _) = CreateRunningGameWithActiveRound();
        GameParticipant participant = game.Participants.Single(p => p.UserId == userId);

        game.SkipCurrentRoundAndAdvance(userId, AppendNextRound(game));

        Assert.Equal(0, participant.Score);
    }

    [Fact]
    public void SkipCurrentRoundAndAdvance_NoSkipsRemaining_Throws()
    {
        var (game, userId, _) = CreateRunningGameWithActiveRound();
        for (int i = 0; i < GameParticipant.TotalSkips; i++)
            game.SkipCurrentRoundAndAdvance(userId, AppendNextRound(game));

        Guid unreachableProblemId = AppendNextRound(game);
        Assert.Throws<InvalidOperationException>(() => game.SkipCurrentRoundAndAdvance(userId, unreachableProblemId));
    }

    [Fact]
    public void SkipCurrentRoundAndAdvance_NotOnARound_ThrowsWithoutConsumingASkip()
    {
        Guid userId = Guid.NewGuid();
        Game game = new(GameModeId, PoolId, [TrackId], [userId, Guid.NewGuid()], timeLimitInSeconds: 300);
        game.Start();
        Guid nextProblemId = AppendNextRound(game);
        GameParticipant participant = game.Participants.Single(p => p.UserId == userId);

        Assert.Throws<InvalidOperationException>(() => game.SkipCurrentRoundAndAdvance(userId, nextProblemId));

        Assert.Equal(GameParticipant.TotalSkips, participant.SkipsRemaining);
    }

    [Fact]
    public void ExcludedProblemIdsFor_IncludesSkippedProblems_NotJustSolved()
    {
        var (game, userId, firstProblemId) = CreateRunningGameWithActiveRound();
        Guid secondProblemId = AppendNextRound(game);
        game.SkipCurrentRoundAndAdvance(userId, secondProblemId);

        Assert.Contains(firstProblemId, game.ExcludedProblemIdsFor(userId));
    }

    [Fact]
    public void ExcludedProblemIdsFor_IncludesTheCurrentProblem()
    {
        var (game, userId, problemId) = CreateRunningGameWithActiveRound();

        Assert.Contains(problemId, game.ExcludedProblemIdsFor(userId));
    }

    [Fact]
    public void SolveCurrentRoundAndAdvance_PreservesPreviouslySkippedProblems()
    {
        var (game, userId, _) = CreateRunningGameWithActiveRound();
        Guid skippedProblemId = AppendNextRound(game);
        game.SkipCurrentRoundAndAdvance(userId, skippedProblemId);
        Guid solvedProblemId = AppendNextRound(game);
        game.SkipCurrentRoundAndAdvance(userId, solvedProblemId);

        game.SolveCurrentRoundAndAdvance(userId, AppendNextRound(game), Guid.NewGuid());

        GameParticipant participant = game.Participants.Single(p => p.UserId == userId);
        GameRoundResult progress = RoundResultFor(game, participant, skippedProblemId);
        Assert.Equal(GameRoundOutcome.Skipped, progress.Outcome);
    }

    [Fact]
    public void SolveCurrentRoundAndAdvance_WithEmptySubmissionId_Throws()
    {
        var (game, userId, _) = CreateRunningGameWithActiveRound();
        Guid nextProblemId = AppendNextRound(game);

        Assert.Throws<ArgumentException>(() =>
            game.SolveCurrentRoundAndAdvance(userId, nextProblemId, Guid.Empty)
        );
    }

    [Fact]
    public void SolveCurrentRoundAndAdvance_RecordsTheSolvingSubmission()
    {
        var (game, userId, solvedProblemId) = CreateRunningGameWithActiveRound();
        GameParticipant participant = game.Participants.Single(p => p.UserId == userId);
        var submissionId = Guid.NewGuid();

        game.SolveCurrentRoundAndAdvance(userId, AppendNextRound(game), submissionId);

        GameRoundResult result = RoundResultFor(game, participant, solvedProblemId);
        Assert.Equal(GameRoundOutcome.Solved, result.Outcome);
        Assert.Equal(submissionId, result.SolvingSubmissionId);
    }

    [Fact]
    public void SkipCurrentRoundAndAdvance_WithNoSubmission_IsAllowed()
    {
        var (game, userId, skippedProblemId) = CreateRunningGameWithActiveRound();
        GameParticipant participant = game.Participants.Single(p => p.UserId == userId);

        game.SkipCurrentRoundAndAdvance(userId, AppendNextRound(game));

        GameRoundResult progress = RoundResultFor(game, participant, skippedProblemId);
        Assert.Equal(GameRoundOutcome.Skipped, progress.Outcome);
        Assert.Null(progress.SolvingSubmissionId);
    }

    [Fact]
    public void UseSkip_DecrementsSkipsRemaining()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        participant.UseSkip();

        Assert.Equal(GameParticipant.TotalSkips - 1, participant.SkipsRemaining);
    }

    [Fact]
    public void UseSkip_NoSkipsRemaining_Throws()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);
        for (int i = 0; i < GameParticipant.TotalSkips; i++)
            participant.UseSkip();

        Assert.Throws<InvalidOperationException>(() => participant.UseSkip());
    }

    [Fact]
    public void PlayState_FreshParticipant_IsInProgress()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.Equal(ParticipantPlayState.InProgress, participant.PlayState);
    }

    [Fact]
    public void PlayState_AfterForfeit_IsForfeited()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        participant.Forfeit();

        Assert.Equal(ParticipantPlayState.Forfeited, participant.PlayState);
    }

    [Fact]
    public void PlayState_AfterFinishProblems_IsFinished()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        participant.FinishProblems();

        Assert.Equal(ParticipantPlayState.Finished, participant.PlayState);
    }

    [Theory]
    [InlineData(ParticipantAction.CompleteProblem)]
    [InlineData(ParticipantAction.SkipProblem)]
    [InlineData(ParticipantAction.Forfeit)]
    public void CanPerform_WhileInProgress_AllowsEveryAction(ParticipantAction action)
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.True(participant.CanPerform(action));
    }

    [Theory]
    [InlineData(ParticipantAction.CompleteProblem)]
    [InlineData(ParticipantAction.SkipProblem)]
    [InlineData(ParticipantAction.Forfeit)]
    public void CanPerform_AfterForfeit_DisallowsEveryAction(ParticipantAction action)
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);
        participant.Forfeit();

        Assert.False(participant.CanPerform(action));
    }

    [Theory]
    [InlineData(ParticipantAction.CompleteProblem)]
    [InlineData(ParticipantAction.SkipProblem)]
    [InlineData(ParticipantAction.Forfeit)]
    public void CanPerform_AfterFinishProblems_DisallowsEveryAction(ParticipantAction action)
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);
        participant.FinishProblems();

        Assert.False(participant.CanPerform(action));
    }

    [Fact]
    public void EvaluateProblemAttempt_OnCurrentProblem_ReturnsEligible()
    {
        var (game, userId, problemId) = CreateRunningGameWithActiveRound();

        var eligibility = game
            .EvaluateProblemAttempt(userId, problemId, ParticipantAction.CompleteProblem, DateTime.UtcNow)
            .Eligibility;

        Assert.Equal(ProblemAttemptEligibility.Eligible, eligibility);
    }

    [Fact]
    public void EvaluateProblemAttempt_NoRoundStarted_ReturnsRoundNotStarted()
    {
        Guid userId = Guid.NewGuid();
        Game game = new(GameModeId, PoolId, [TrackId], [userId], timeLimitInSeconds: 300);
        game.Start();

        var eligibility = game
            .EvaluateProblemAttempt(userId, Guid.NewGuid(), ParticipantAction.CompleteProblem, DateTime.UtcNow)
            .Eligibility;

        Assert.Equal(ProblemAttemptEligibility.RoundNotStarted, eligibility);
    }

    [Fact]
    public void EvaluateProblemAttempt_DifferentProblemId_ReturnsProblemMismatch()
    {
        var (game, userId, _) = CreateRunningGameWithActiveRound();

        var eligibility = game
            .EvaluateProblemAttempt(userId, Guid.NewGuid(), ParticipantAction.CompleteProblem, DateTime.UtcNow)
            .Eligibility;

        Assert.Equal(ProblemAttemptEligibility.ProblemMismatch, eligibility);
    }

    [Fact]
    public void EvaluateProblemAttempt_AfterForfeit_ReturnsParticipantStopped()
    {
        var (game, userId, problemId) = CreateRunningGameWithActiveRound();
        game.Forfeit(userId);

        var eligibility = game
            .EvaluateProblemAttempt(userId, problemId, ParticipantAction.CompleteProblem, DateTime.UtcNow)
            .Eligibility;

        Assert.Equal(ProblemAttemptEligibility.ParticipantStopped, eligibility);
    }
}