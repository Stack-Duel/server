using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Domain.Tests.Submissions.Entities;

public class SubmissionResultTests
{
    private static readonly SourceCode ValidSourceCode = new("int main() {}");

    private static Submission CreateSubmission(params Guid[] testCaseIds) =>
        new(Guid.NewGuid(), Guid.NewGuid(), SubmissionType.Submit, ValidSourceCode, testCaseIds);

    [Test]
    public void InitialStatus_IsPending()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        var result = submission.Results.First();

        Assert.That(result.Status, Is.EqualTo(SubmissionResultStatus.Pending));
    }

    [Test]
    public void IsTerminal_WhenPending_IsFalse()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        Assert.That(submission.Results.First().IsTerminal, Is.False);
    }

    [Test]
    public void IsTerminal_WhenProcessing_IsFalse()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);
        submission.UpdateResult(testCaseId, SubmissionResultStatus.Processing);

        Assert.That(submission.Results.First().IsTerminal, Is.False);
    }

    [TestCase(SubmissionResultStatus.Accepted)]
    [TestCase(SubmissionResultStatus.WrongAnswer)]
    [TestCase(SubmissionResultStatus.TimeLimitExceeded)]
    [TestCase(SubmissionResultStatus.MemoryLimitExceeded)]
    [TestCase(SubmissionResultStatus.RuntimeError)]
    [TestCase(SubmissionResultStatus.CompileError)]
    public void IsTerminal_WhenTerminalStatus_IsTrue(SubmissionResultStatus status)
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);
        submission.UpdateResult(testCaseId, status);

        Assert.That(submission.Results.First().IsTerminal, Is.True);
    }

    [Test]
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(SubmissionResultStatus.WrongAnswer));
            Assert.That(result.Runtime, Is.EqualTo(150));
            Assert.That(result.MemoryUsed, Is.EqualTo(64));
            Assert.That(result.ActualOutput, Is.EqualTo("wrong"));
            Assert.That(result.StandardOutput, Is.EqualTo("stdout"));
            Assert.That(result.StandardError, Is.EqualTo("stderr"));
            Assert.That(result.CompileOutput, Is.EqualTo("compile"));
        }
    }

    [Test]
    public void Update_NullableFields_DefaultToNull()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        submission.UpdateResult(testCaseId, SubmissionResultStatus.Accepted);

        var result = submission.Results.First();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Runtime, Is.Null);
            Assert.That(result.MemoryUsed, Is.Null);
            Assert.That(result.ActualOutput, Is.Null);
            Assert.That(result.StandardOutput, Is.Null);
            Assert.That(result.StandardError, Is.Null);
            Assert.That(result.CompileOutput, Is.Null);
        }
    }

    [Test]
    public void Update_CalledTwice_OverwritesPreviousValues()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);
        submission.UpdateResult(testCaseId, SubmissionResultStatus.Processing, runtime: 100);

        submission.UpdateResult(testCaseId, SubmissionResultStatus.Accepted, runtime: 200);

        var result = submission.Results.First();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(SubmissionResultStatus.Accepted));
            Assert.That(result.Runtime, Is.EqualTo(200));
        }
    }

    [Test]
    public void TestCaseId_SetCorrectly()
    {
        var testCaseId = Guid.NewGuid();
        var submission = CreateSubmission(testCaseId);

        Assert.That(submission.Results.First().TestCaseId, Is.EqualTo(testCaseId));
    }
}