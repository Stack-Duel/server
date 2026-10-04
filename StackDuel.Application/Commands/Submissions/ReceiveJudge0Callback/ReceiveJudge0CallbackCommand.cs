using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback;

public sealed record ReceiveJudge0CallbackCommand(string? Key, Guid SubmissionId) : ICommand;