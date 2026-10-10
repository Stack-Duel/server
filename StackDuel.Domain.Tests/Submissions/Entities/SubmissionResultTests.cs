using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Domain.Tests.Submissions.Entities;

public class SubmissionResultTests
{
    private static readonly SourceCode ValidSourceCode = new("int main() {}");

    private static Submission CreateSubmission(params Guid[] testCaseIds) =>
        new(Guid.NewGuid(), Guid.NewGuid(), SubmissionType.Submit, ValidSourceCode, testCaseIds);

    [Fact]
    public void InitialStatus_IsPending()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        var result = submission.Results.First();

        Assert.Equal(SubmissionResultStatus.Pending, result.Status);
    }

    [Fact]
    public void IsTerminal_WhenPending_IsFalse()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        Assert.False(submission.Results.First().IsTerminal);
    }

    [Fact]
    public void IsTerminal_WhenProcessing_IsFalse()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);
        submission.UpdateResult(testCaseId, SubmissionResultStatus.Processing);

        Assert.False(submission.Results.First().IsTerminal);
    }

    [Theory]
    [InlineData(SubmissionResultStatus.Accepted)]
    [InlineData(SubmissionResultStatus.WrongAnswer)]
    [InlineData(SubmissionResultStatus.TimeLimitExceeded)]
    [InlineData(SubmissionResultStatus.MemoryLimitExceeded)]
    [InlineData(SubmissionResultStatus.RuntimeError)]
    [InlineData(SubmissionResultStatus.CompileError)]
    public void IsTerminal_WhenTerminalStatus_IsTrue(SubmissionResultStatus status)
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);
        submission.UpdateResult(testCaseId, status);

        Assert.True(submission.Results.First().IsTerminal);
    }

    [Fact]
    public void Update_SetsAllOutputFields()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        submission.UpdateResult(
            testCaseId,
            SubmissionResultStatus.WrongAnswer,
            runtime: 150,
            memoryUsed: 64,
            actualOutput: "wrong",
            standardOutput: "stdout",
            standardError: "stderr",
            compileOutput: "compile"
        );

        var result = submission.Results.First();
        Assert.Equal(SubmissionResultStatus.WrongAnswer, result.Status);
        Assert.Equal(150, result.Runtime);
        Assert.Equal(64, result.MemoryUsed);
        Assert.Equal("wrong", result.ActualOutput);
        Assert.Equal("stdout", result.StandardOutput);
        Assert.Equal("stderr", result.StandardError);
        Assert.Equal("compile", result.CompileOutput);
    }

    [Fact]
    public void Update_NullableFields_DefaultToNull()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        submission.UpdateResult(testCaseId, SubmissionResultStatus.Accepted);

        var result = submission.Results.First();
        Assert.Null(result.Runtime);
        Assert.Null(result.MemoryUsed);
        Assert.Null(result.ActualOutput);
        Assert.Null(result.StandardOutput);
        Assert.Null(result.StandardError);
        Assert.Null(result.CompileOutput);
    }

    [Fact]
    public void Update_CalledTwice_OverwritesPreviousValues()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);
        submission.UpdateResult(testCaseId, SubmissionResultStatus.Processing, runtime: 100);

        submission.UpdateResult(testCaseId, SubmissionResultStatus.Accepted, runtime: 200);

        var result = submission.Results.First();
        Assert.Equal(SubmissionResultStatus.Accepted, result.Status);
        Assert.Equal(200, result.Runtime);
    }

    [Fact]
    public void TestCaseId_SetCorrectly()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        Assert.Equal(testCaseId, submission.Results.First().TestCaseId);
    }
}