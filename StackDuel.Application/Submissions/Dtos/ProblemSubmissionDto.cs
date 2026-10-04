using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions.Dtos;

public sealed record SubmissionUserDto(string Username, string? ImageUrl);

public sealed record SubmissionLanguageDto(Guid Id, string Name, string Version);

public sealed record ProblemSubmissionDto(
    Guid Id,
    SubmissionStatus Status,
    SubmissionLanguageDto Language,
    string Code,
    DateTime CreatedAt,
    SubmissionUserDto User,
    int? MemoryUsage,
    int? ExecutionTime
);