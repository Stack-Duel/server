using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Domain.Tests.Games.Entities;

public class GameParticipantTests
{
    private static GameParticipant CreateParticipantWithProblem(out Guid problemId)
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);
        problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);
        return participant;
    }

    [Fact]
    public void Constructor_StartsWithTotalSkipsRemaining()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.Equal(GameParticipant.TotalSkips, participant.SkipsRemaining);
    }

    [Fact]
    public void SkipToProblem_DecrementsSkipsRemaining()
    {
        var participant = CreateParticipantWithProblem(out _);

        participant.SkipToProblem(Guid.NewGuid());

        Assert.Equal(GameParticipant.TotalSkips - 1, participant.SkipsRemaining);
    }

    [Fact]
    public void SkipToProblem_MovesCurrentProblemToSkipped_NotSolved()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);

        participant.SkipToProblem(Guid.NewGuid());

        Assert.Contains(problemId, participant.ProblemSession!.SkippedProblemIds);
        Assert.DoesNotContain(problemId, participant.ProblemSession.SolvedProblemIds);
    }

    [Fact]
    public void SkipToProblem_SetsCurrentProblemToTheNewProblem()
    {
        var participant = CreateParticipantWithProblem(out _);
        Guid nextProblemId = Guid.NewGuid();

        participant.SkipToProblem(nextProblemId);

        Assert.Equal(nextProblemId, participant.ProblemSession!.CurrentProblemId);
    }

    [Fact]
    public void SkipToProblem_DoesNotAffectScore()
    {
        var participant = CreateParticipantWithProblem(out _);

        participant.SkipToProblem(Guid.NewGuid());

        Assert.Equal(0, participant.Score);
    }

    [Fact]
    public void SkipToProblem_NoSkipsRemaining_Throws()
    {
        var participant = CreateParticipantWithProblem(out _);
        for (int i = 0; i < GameParticipant.TotalSkips; i++)
            participant.SkipToProblem(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => participant.SkipToProblem(Guid.NewGuid()));
    }

    [Fact]
    public void SkipToProblem_ProblemSessionNotInitialized_Throws()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.Throws<InvalidOperationException>(() => participant.SkipToProblem(Guid.NewGuid()));
    }

    [Fact]
    public void ExcludedProblemIds_IncludesSkippedProblems_NotJustSolved()
    {
        var participant = CreateParticipantWithProblem(out Guid firstProblemId);
        var secondProblemId = Guid.NewGuid();
        participant.SkipToProblem(secondProblemId);

        Assert.Contains(firstProblemId, participant.ProblemSession!.ExcludedProblemIds);
    }

    [Fact]
    public void ExcludedProblemIds_IncludesTheCurrentProblem()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);

        Assert.Contains(problemId, participant.ProblemSession!.ExcludedProblemIds);
    }

    [Fact]
    public void AdvanceProblem_PreservesPreviouslySkippedProblems()
    {
        var participant = CreateParticipantWithProblem(out _);
        var skippedProblemId = Guid.NewGuid();
        participant.SkipToProblem(skippedProblemId);
        // Now on skippedProblemId; skip again so it lands in SkippedProblemIds, then solve
        // the one after that and confirm the earlier skip survives the AdvanceProblem rebuild.
        var solvedProblemId = Guid.NewGuid();
        participant.SkipToProblem(solvedProblemId);

        participant.AdvanceProblem(Guid.NewGuid());

        Assert.Contains(skippedProblemId, participant.ProblemSession!.SkippedProblemIds);
    }

    [Fact]
    public void SetActiveSubmission_PreservesPreviouslySkippedProblems()
    {
        var participant = CreateParticipantWithProblem(out Guid skippedProblemId);
        participant.SkipToProblem(Guid.NewGuid());

        participant.SetActiveSubmission(Guid.NewGuid());

        Assert.Contains(skippedProblemId, participant.ProblemSession!.SkippedProblemIds);
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
    public void CanAttemptProblem_OnCurrentProblem_ReturnsEligible()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, problemId);

        Assert.Equal(ProblemAttemptEligibility.Eligible, eligibility);
    }

    [Fact]
    public void CanAttemptProblem_NoSessionInitialized_ReturnsProblemSessionNotInitialized()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, Guid.NewGuid());

        Assert.Equal(ProblemAttemptEligibility.ProblemSessionNotInitialized, eligibility);
    }

    [Fact]
    public void CanAttemptProblem_DifferentProblemId_ReturnsProblemMismatch()
    {
        var participant = CreateParticipantWithProblem(out _);

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, Guid.NewGuid());

        Assert.Equal(ProblemAttemptEligibility.ProblemMismatch, eligibility);
    }

    [Fact]
    public void CanAttemptProblem_AfterForfeit_ReturnsParticipantStopped()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);
        participant.Forfeit();

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, problemId);

        Assert.Equal(ProblemAttemptEligibility.ParticipantStopped, eligibility);
    }
}