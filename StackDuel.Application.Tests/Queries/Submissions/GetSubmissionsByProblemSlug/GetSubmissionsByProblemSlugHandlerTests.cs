using Ardalis.Result;
using Moq;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using GetSubmissionsByProblemSlugHandler = StackDuel.Application.Queries.Submissions.GetSubmissionsByProblemSlug.GetSubmissionsByProblemSlugHandler;
using GetSubmissionsByProblemSlugQuery = StackDuel.Application.Queries.Submissions.GetSubmissionsByProblemSlug.GetSubmissionsByProblemSlugQuery;

namespace StackDuel.Application.Tests.Queries.Submissions.GetSubmissionsByProblemSlug;

public class GetSubmissionsByProblemSlugHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;
    private GetSubmissionsByProblemSlugHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();
        _handler = new GetSubmissionsByProblemSlugHandler(
            _problemReadRepository.Object,
            _submissionReadRepository.Object
        );
    }

    [Test]
    public async Task Handle_SlugNotFound_ReturnsNotFound()
    {
        _problemReadRepository
            .Setup(x => x.GetIdBySlugAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var query = new GetSubmissionsByProblemSlugQuery(
            "missing",
            new PaginationRequest { Page = 1, Size = 10 },
            null,
            SubmissionFilter.MySubmissions,
            SubmissionSortOrder.Newest
        );

        Result<PageResult<ProblemSubmissionDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
        _submissionReadRepository.Verify(
            x =>
                x.GetProblemSubmissionsPagedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<SubmissionFilter>(),
                    It.IsAny<SubmissionSortOrder>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_SlugFound_PassesResolvedProblemIdAndFiltersToRepository()
    {
        var problemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pagination = new PaginationRequest { Page = 2, Size = 20 };
        var expected = new PageResult<ProblemSubmissionDto>
        {
            Results = [],
            Total = 0,
            Page = 2,
            Size = 20,
        };

        _problemReadRepository
            .Setup(x => x.GetIdBySlugAsync("two-sum", It.IsAny<CancellationToken>()))
            .ReturnsAsync(problemId);
        _submissionReadRepository
            .Setup(x =>
                x.GetProblemSubmissionsPagedAsync(
                    problemId,
                    pagination,
                    userId,
                    SubmissionFilter.UserSolutions,
                    SubmissionSortOrder.Oldest,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expected);

        var query = new GetSubmissionsByProblemSlugQuery(
            "two-sum",
            pagination,
            userId,
            SubmissionFilter.UserSolutions,
            SubmissionSortOrder.Oldest
        );

        Result<PageResult<ProblemSubmissionDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.SameAs(expected));
    }
}