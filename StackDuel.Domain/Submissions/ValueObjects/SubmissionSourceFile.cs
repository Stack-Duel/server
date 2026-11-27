namespace StackDuel.Domain.Submissions.ValueObjects;

public sealed record SubmissionSourceFile(string Path, string Content)
{
    public static readonly int MaxPathLength = 100;

    public static readonly int MaxContentLength = 65536;

    public static readonly int MaxFilesPerSubmission = 10;
}