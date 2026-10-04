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

    [Test]
    public void Constructor_StartsWithTotalSkipsRemaining()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.That(participant.SkipsRemaining, Is.EqualTo(GameParticipant.TotalSkips));
    }

    [Test]
    public void SkipToProblem_DecrementsSkipsRemaining()
    {
        var participant = CreateParticipantWithProblem(out _);

        participant.SkipToProblem(Guid.NewGuid());

        Assert.That(participant.SkipsRemaining, Is.EqualTo(GameParticipant.TotalSkips - 1));
    }

    [Test]
    public void SkipToProblem_MovesCurrentProblemToSkipped_NotSolved()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);

        participant.SkipToProblem(Guid.NewGuid());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(participant.ProblemSession!.SkippedProblemIds, Does.Contain(problemId));
            Assert.That(participant.ProblemSession.SolvedProblemIds, Does.Not.Contain(problemId));
        }
    }

    [Test]
    public void SkipToProblem_SetsCurrentProblemToTheNewProblem()
    {
        var participant = CreateParticipantWithProblem(out _);
        Guid nextProblemId = Guid.NewGuid();

        participant.SkipToProblem(nextProblemId);

        Assert.That(participant.ProblemSession!.CurrentProblemId, Is.EqualTo(nextProblemId));
    }

    [Test]
    public void SkipToProblem_DoesNotAffectScore()
    {
        var participant = CreateParticipantWithProblem(out _);

        participant.SkipToProblem(Guid.NewGuid());

        Assert.That(participant.Score, Is.EqualTo(0));
    }

    [Test]
    public void SkipToProblem_NoSkipsRemaining_Throws()
    {
        var participant = CreateParticipantWithProblem(out _);
        for (int i = 0; i < GameParticipant.TotalSkips; i++)
            participant.SkipToProblem(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => participant.SkipToProblem(Guid.NewGuid()));
    }

    [Test]
    public void SkipToProblem_ProblemSessionNotInitialized_Throws()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.Throws<InvalidOperationException>(() => participant.SkipToProblem(Guid.NewGuid()));
    }

    [Test]
    public void ExcludedProblemIds_IncludesSkippedProblems_NotJustSolved()
    {
        var participant = CreateParticipantWithProblem(out Guid firstProblemId);
        var secondProblemId = Guid.NewGuid();
        participant.SkipToProblem(secondProblemId);

        Assert.That(participant.ProblemSession!.ExcludedProblemIds, Does.Contain(firstProblemId));
    }

    [Test]
    public void ExcludedProblemIds_IncludesTheCurrentProblem()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);

        Assert.That(participant.ProblemSession!.ExcludedProblemIds, Does.Contain(problemId));
    }

    [Test]
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

        Assert.That(participant.ProblemSession!.SkippedProblemIds, Does.Contain(skippedProblemId));
    }

    [Test]
    public void SetActiveSubmission_PreservesPreviouslySkippedProblems()
    {
        var participant = CreateParticipantWithProblem(out Guid skippedProblemId);
        participant.SkipToProblem(Guid.NewGuid());

        participant.SetActiveSubmission(Guid.NewGuid());

        Assert.That(participant.ProblemSession!.SkippedProblemIds, Does.Contain(skippedProblemId));
    }

    [Test]
    public void UseSkip_DecrementsSkipsRemaining()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        participant.UseSkip();

        Assert.That(participant.SkipsRemaining, Is.EqualTo(GameParticipant.TotalSkips - 1));
    }

    [Test]
    public void UseSkip_NoSkipsRemaining_Throws()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);
        for (int i = 0; i < GameParticipant.TotalSkips; i++)
            participant.UseSkip();

        Assert.Throws<InvalidOperationException>(() => participant.UseSkip());
    }

    [Test]
    public void PlayState_FreshParticipant_IsInProgress()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.That(participant.PlayState, Is.EqualTo(ParticipantPlayState.InProgress));
    }

    [Test]
    public void PlayState_AfterForfeit_IsForfeited()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        participant.Forfeit();

        Assert.That(participant.PlayState, Is.EqualTo(ParticipantPlayState.Forfeited));
    }

    [Test]
    public void PlayState_AfterFinishProblems_IsFinished()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        participant.FinishProblems();

        Assert.That(participant.PlayState, Is.EqualTo(ParticipantPlayState.Finished));
    }

    [Test]
    [TestCase(ParticipantAction.CompleteProblem)]
    [TestCase(ParticipantAction.SkipProblem)]
    [TestCase(ParticipantAction.Forfeit)]
    public void CanPerform_WhileInProgress_AllowsEveryAction(ParticipantAction action)
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        Assert.That(participant.CanPerform(action), Is.True);
    }

    [Test]
    [TestCase(ParticipantAction.CompleteProblem)]
    [TestCase(ParticipantAction.SkipProblem)]
    [TestCase(ParticipantAction.Forfeit)]
    public void CanPerform_AfterForfeit_DisallowsEveryAction(ParticipantAction action)
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);
        participant.Forfeit();

        Assert.That(participant.CanPerform(action), Is.False);
    }

    [Test]
    [TestCase(ParticipantAction.CompleteProblem)]
    [TestCase(ParticipantAction.SkipProblem)]
    [TestCase(ParticipantAction.Forfeit)]
    public void CanPerform_AfterFinishProblems_DisallowsEveryAction(ParticipantAction action)
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);
        participant.FinishProblems();

        Assert.That(participant.CanPerform(action), Is.False);
    }

    [Test]
    public void CanAttemptProblem_OnCurrentProblem_ReturnsEligible()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, problemId);

        Assert.That(eligibility, Is.EqualTo(ProblemAttemptEligibility.Eligible));
    }

    [Test]
    public void CanAttemptProblem_NoSessionInitialized_ReturnsProblemSessionNotInitialized()
    {
        var participant = new GameParticipant(Guid.NewGuid(), seatNo: 1);

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, Guid.NewGuid());

        Assert.That(eligibility, Is.EqualTo(ProblemAttemptEligibility.ProblemSessionNotInitialized));
    }

    [Test]
    public void CanAttemptProblem_DifferentProblemId_ReturnsProblemMismatch()
    {
        var participant = CreateParticipantWithProblem(out _);

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, Guid.NewGuid());

        Assert.That(eligibility, Is.EqualTo(ProblemAttemptEligibility.ProblemMismatch));
    }

    [Test]
    public void CanAttemptProblem_AfterForfeit_ReturnsParticipantStopped()
    {
        var participant = CreateParticipantWithProblem(out Guid problemId);
        participant.Forfeit();

        var eligibility = participant.CanAttemptProblem(ParticipantAction.CompleteProblem, problemId);

        Assert.That(eligibility, Is.EqualTo(ProblemAttemptEligibility.ParticipantStopped));
    }
}