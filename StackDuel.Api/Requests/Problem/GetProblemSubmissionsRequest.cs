using StackDuel.Application.Submissions;

namespace StackDuel.Api.Requests.Problem;

public sealed record GetProblemSubmissionsRequest(
    int Page,
    int Size,
    DateTime Timestamp,
    SubmissionFilter Type = SubmissionFilter.MySubmissions,
    SubmissionSortOrder SortBy = SubmissionSortOrder.Newest
);