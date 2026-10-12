namespace StackDuel.Application.Submissions;

/// <summary>
/// How long a submission may sit in a non-terminal state before it is treated as abandoned.
/// Shared by the cleanup sweep that fails stale jobs and by callers that refuse to act while a
/// submission is still plausibly being graded, so a submission can never be both "too old to wait
/// for" and "too young to give up on".
/// </summary>
public static class SubmissionGradingWindow
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);
}