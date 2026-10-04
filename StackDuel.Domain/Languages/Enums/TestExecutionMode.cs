namespace StackDuel.Domain.Languages.Enums;

/// <summary>
/// How a language version executes a submission's test cases against Judge0.
/// <see cref="PerCase"/> is today's only mode: one Judge0 submission per test case, each
/// isolated in its own sandboxed process with Judge0's own wall-clock/memory limits.
/// <see cref="BatchedTestFramework"/> runs every test case in a single Judge0 submission via
/// the language's native test runner (e.g. Vitest for JavaScript, NUnit for C#) — far fewer
/// Judge0 calls, at the cost of losing Judge0's per-case process isolation; the harness itself
/// has to provide whatever per-case isolation it still needs.
/// </summary>
public enum TestExecutionMode
{
    PerCase = 0,
    BatchedTestFramework = 1,
}