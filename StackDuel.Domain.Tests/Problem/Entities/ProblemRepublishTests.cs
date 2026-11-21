using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using ProblemEntity = StackDuel.Domain.Problems.Entities.Problem;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemRepublishTests
{
    private static ProblemEntity CreateProblem() =>
        new(
            new Slug("count-vowels"),
            new Title("Count Vowels"),
            new Question(new string('q', 60)),
            new Difficulty(300),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    private static ProblemEntity CreatePublishedProblem()
    {
        ProblemEntity problem = CreateProblem();
        problem.AddSetup(Guid.NewGuid(), "starter", "countVowels", Guid.NewGuid());
        problem.CompleteValidation();
        return problem;
    }

    private static void Retitle(ProblemEntity problem, string title) =>
        problem.UpdateContent(
            new Title(title),
            problem.Question,
            problem.Difficulty,
            problem.TimeLimit,
            problem.MemoryLimit
        );

    [Fact]
    public void EditingADraft_DoesNotFlagUnpublishedChanges()
    {
        ProblemEntity problem = CreateProblem();

        Retitle(problem, "Count Vowels 2");

        Assert.False(problem.HasUnpublishedChanges);
    }

    [Fact]
    public void EditingAPublishedProblem_FlagsUnpublishedChanges()
    {
        ProblemEntity problem = CreatePublishedProblem();

        Retitle(problem, "Count Vowels 2");

        Assert.True(problem.HasUnpublishedChanges);
        Assert.Equal(ProblemStatus.Published, problem.Status);
    }

    [Fact]
    public void PublishedProblemWithNoChanges_CannotBeSubmitted()
    {
        ProblemEntity problem = CreatePublishedProblem();

        Assert.False(problem.CanSubmitForValidation);
    }

    [Fact]
    public void PublishedProblemWithChanges_CanBeSubmitted()
    {
        ProblemEntity problem = CreatePublishedProblem();

        Retitle(problem, "Count Vowels 2");

        Assert.True(problem.CanSubmitForValidation);
    }

    [Fact]
    public void SubmittingAPublishedProblem_KeepsItLiveAndLocksEditing()
    {
        ProblemEntity problem = CreatePublishedProblem();
        Retitle(problem, "Count Vowels 2");

        problem.SubmitForValidation();

        Assert.Equal(ProblemStatus.Published, problem.Status);
        Assert.True(problem.IsRevalidating);
        Assert.True(problem.IsEditLocked);
        Assert.False(problem.CanSubmitForValidation);
    }

    [Fact]
    public void SubmittingADraft_StillMovesItToPending()
    {
        ProblemEntity problem = CreateProblem();

        problem.SubmitForValidation();

        Assert.Equal(ProblemStatus.Pending, problem.Status);
        Assert.False(problem.IsRevalidating);
        Assert.True(problem.IsEditLocked);
    }

    [Fact]
    public void RevalidationSucceeding_FreezesTheNextVersionAndClearsTheFlags()
    {
        ProblemEntity problem = CreatePublishedProblem();
        Retitle(problem, "Count Vowels 2");
        problem.SubmitForValidation();

        problem.CompleteValidation();

        Assert.Equal(ProblemStatus.Published, problem.Status);
        Assert.False(problem.IsRevalidating);
        Assert.False(problem.HasUnpublishedChanges);
        Assert.False(problem.IsEditLocked);
        Assert.Equal(2, problem.Versions.Count);
        Assert.Equal("Count Vowels 2", problem.PublishedVersion!.Title.Value);
    }

    [Fact]
    public void RevalidationFailing_LeavesTheProblemPublishedOnTheOldVersion()
    {
        ProblemEntity problem = CreatePublishedProblem();
        Retitle(problem, "Count Vowels 2");
        problem.SubmitForValidation();

        problem.FailValidation("JavaScript: SyntaxError");

        Assert.Equal(ProblemStatus.Published, problem.Status);
        Assert.False(problem.IsRevalidating);
        Assert.True(problem.HasUnpublishedChanges);
        Assert.Equal("JavaScript: SyntaxError", problem.ValidationFailureReason);
        Assert.Equal("Count Vowels", problem.PublishedVersion!.Title.Value);
        Assert.Single(problem.Versions);
    }

    [Fact]
    public void RevalidationFailing_LeavesItSubmittableAgain()
    {
        ProblemEntity problem = CreatePublishedProblem();
        Retitle(problem, "Count Vowels 2");
        problem.SubmitForValidation();

        problem.FailValidation("JavaScript: SyntaxError");

        Assert.True(problem.CanSubmitForValidation);
        Assert.False(problem.IsEditLocked);
    }

    [Fact]
    public void CancellingARevalidation_LeavesTheProblemPublished()
    {
        ProblemEntity problem = CreatePublishedProblem();
        Retitle(problem, "Count Vowels 2");
        problem.SubmitForValidation();

        problem.CancelValidation();

        Assert.Equal(ProblemStatus.Published, problem.Status);
        Assert.False(problem.IsRevalidating);
        Assert.True(problem.HasUnpublishedChanges);
        Assert.Null(problem.ValidationFailureReason);
    }

    [Fact]
    public void CancellingADraftValidation_StillReturnsItToDraft()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();

        problem.CancelValidation();

        Assert.Equal(ProblemStatus.Draft, problem.Status);
    }

    [Fact]
    public void SetupEditsOnAPublishedProblem_FlagUnpublishedChanges()
    {
        ProblemEntity problem = CreatePublishedProblem();
        ProblemSetup setup = problem.Setups.First();

        problem.UpdateSetupInitialCode(setup.Id, "export function countVowels(text) {}");

        Assert.True(problem.HasUnpublishedChanges);
    }

    [Fact]
    public void GenerationParameterEditsOnAPublishedProblem_FlagUnpublishedChanges()
    {
        ProblemEntity problem = CreatePublishedProblem();

        problem.SetGenerationParameters(
            [new GenerationParameterSpec("text", "string", null, null, 1, 24, "abc")],
            "integer",
            20,
            7
        );

        Assert.True(problem.HasUnpublishedChanges);
    }

    [Fact]
    public void SampleTestCaseEditsOnAPublishedProblem_FlagUnpublishedChanges()
    {
        ProblemEntity problem = CreatePublishedProblem();

        problem.MarkSampleTestCasesChanged();

        Assert.True(problem.HasUnpublishedChanges);
    }
}