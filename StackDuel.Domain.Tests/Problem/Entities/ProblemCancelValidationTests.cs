using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using ProblemEntity = StackDuel.Domain.Problems.Entities.Problem;

namespace StackDuel.Domain.Tests.Problem.Entities;

public class ProblemCancelValidationTests
{
    private static ProblemEntity CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('a', Question.MinLength)),
            new Difficulty(500),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    [Fact]
    public void CancelValidation_WhilePending_ReturnsTheProblemToDraft()
    {
        ProblemEntity problem = CreateProblem();
        problem.SubmitForValidation();

        problem.CancelValidation();

        Assert.Equal(ProblemStatus.Draft, problem.Status);
        Assert.Null(problem.ValidationFailureReason);
    }

    [Fact]
    public void CancelValidation_ClearsAnEarlierFailureReason()
    {
        ProblemEntity problem = CreateProblem();
        problem.FailValidation("Python: wrong answer");
        problem.SubmitForValidation();

        problem.CancelValidation();

        Assert.Null(problem.ValidationFailureReason);
    }

    [Fact]
    public void CancelValidation_WhenPublished_ChangesNothing()
    {
        ProblemEntity problem = CreateProblem();
        problem.Publish();

        problem.CancelValidation();

        Assert.Equal(ProblemStatus.Published, problem.Status);
    }

    [Fact]
    public void CancelValidation_WhenFailed_ChangesNothing()
    {
        ProblemEntity problem = CreateProblem();
        problem.FailValidation("Python: wrong answer");

        problem.CancelValidation();

        Assert.Equal(ProblemStatus.Failed, problem.Status);
        Assert.Equal("Python: wrong answer", problem.ValidationFailureReason);
    }

    [Fact]
    public void FailValidation_TruncatesAReasonTooLongForTheColumn()
    {
        ProblemEntity problem = CreateProblem();
        string reason = new('x', ProblemEntity.MaxValidationFailureReasonLength + 500);

        problem.FailValidation(reason);

        Assert.Equal(ProblemEntity.MaxValidationFailureReasonLength, problem.ValidationFailureReason!.Length);
    }

    [Fact]
    public void FailValidation_KeepsAReasonThatFits()
    {
        ProblemEntity problem = CreateProblem();

        problem.FailValidation("Python: wrong answer on sample case 4");

        Assert.Equal("Python: wrong answer on sample case 4", problem.ValidationFailureReason);
    }

    [Fact]
    public void FailValidation_EmptyReason_Throws()
    {
        ProblemEntity problem = CreateProblem();

        Assert.Throws<ArgumentException>(() => problem.FailValidation("   "));
    }
}