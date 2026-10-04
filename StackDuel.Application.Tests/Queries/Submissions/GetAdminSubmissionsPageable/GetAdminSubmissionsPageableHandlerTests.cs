using StackDuel.Application.Pagination;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using Ardalis.Result;
using Moq;
using GetAdminSubmissionsPageableHandler = StackDuel.Application.Queries.Submissions.GetAdminSubmissionsPageable.GetAdminSubmissionsPageableHandler;
using GetAdminSubmissionsPageableQuery = StackDuel.Application.Queries.Submissions.GetAdminSubmissionsPageable.GetAdminSubmissionsPageableQuery;

namespace StackDuel.Application.Tests.Queries.Submissions.GetAdminSubmissionsPageable;

public class GetAdminSubmissionsPageableHandlerTests
{
    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;
    private GetAdminSubmissionsPageableHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();
        _handler = new GetAdminSubmissionsPageableHandler(_submissionReadRepository.Object);
    }

    [Test]
    public async Task Handle_ReturnsPagedResultFromRepository()
    {
        var pagination = new PaginationRequest { Page = 2, Size = 10 };
        var filterId = Guid.NewGuid();
        var expected = new PageResult<AdminSubmissionListItemDto>
        {
            Results = [],
            Total = 0,
            Page = 2,
            Size = 10,
        };

        _submissionReadRepository
            .Setup(x => x.GetAdminSubmissionsPagedAsync(pagination, filterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        Result<PageResult<AdminSubmissionListItemDto>> result = await _handler.Handle(
            new GetAdminSubmissionsPageableQuery(pagination, filterId),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.SameAs(expected));
    }

    [Test]
    public async Task Handle_NullId_PassesNullThrough()
    {
        var pagination = new PaginationRequest { Page = 1, Size = 25 };

        await _handler.Handle(new GetAdminSubmissionsPageableQuery(pagination, null), CancellationToken.None);

        _submissionReadRepository.Verify(
            x => x.GetAdminSubmissionsPagedAsync(pagination, null, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}