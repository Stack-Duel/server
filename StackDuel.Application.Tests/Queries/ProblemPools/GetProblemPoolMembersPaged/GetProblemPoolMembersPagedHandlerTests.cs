using StackDuel.Application.Languages;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using Ardalis.Result;
using Moq;
using GetProblemPoolMembersPagedHandler = StackDuel.Application.Queries.ProblemPools.GetProblemPoolMembersPaged.GetProblemPoolMembersPagedHandler;
using GetProblemPoolMembersPagedQuery = StackDuel.Application.Queries.ProblemPools.GetProblemPoolMembersPaged.GetProblemPoolMembersPagedQuery;

namespace StackDuel.Application.Tests.Queries.ProblemPools.GetProblemPoolMembersPaged;

public class GetProblemPoolMembersPagedHandlerTests
{
    private Mock<IProblemPoolRepository> _problemPoolRepository = null!;
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private GetProblemPoolMembersPagedHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemPoolRepository = new Mock<IProblemPoolRepository>();
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetProblemPoolMembersPagedHandler(
            _problemPoolRepository.Object,
            _problemReadRepository.Object,
            _languageReadRepository.Object
        );

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static AdminProblemListRowDto CreateRow(Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            "two-sum",
            "Two Sum",
            150,
            ProblemStatus.Published,
            1000,
            256,
            [],
            [],
            0,
            DateTime.UtcNow,
            null
        );

    [Test]
    public async Task Handle_PoolNotFound_ReturnsNotFound()
    {
        _problemPoolRepository
            .Setup(x => x.FindByKeyAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemPool?)null);

        var result = await _handler.Handle(
            new GetProblemPoolMembersPagedQuery("missing", new PaginationRequest { Page = 1, Size = 20 }),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_PoolFound_PassesThroughPageMetadataFromRepository()
    {
        var pool = new ProblemPool("daily", "Daily challenge");
        _problemPoolRepository.Setup(x => x.FindByKeyAsync("daily", It.IsAny<CancellationToken>())).ReturnsAsync(pool);

        var page = new PageResult<AdminProblemListRowDto>
        {
            Results = [CreateRow()],
            Total = 42,
            Page = 2,
            Size = 10,
        };
        _problemReadRepository
            .Setup(x =>
                x.GetPoolMembersPagedAsync(pool.Id, It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(page);

        var pagination = new PaginationRequest { Page = 2, Size = 10 };
        Result<PageResult<AdminProblemListItemDto>> result = await _handler.Handle(
            new GetProblemPoolMembersPagedQuery("daily", pagination),
            CancellationToken.None
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Total, Is.EqualTo(42));
            Assert.That(result.Value.Page, Is.EqualTo(2));
            Assert.That(result.Value.Size, Is.EqualTo(10));
            Assert.That(result.Value.Results, Has.Count.EqualTo(1));
        });
        _problemReadRepository.Verify(
            x => x.GetPoolMembersPagedAsync(pool.Id, pagination, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}