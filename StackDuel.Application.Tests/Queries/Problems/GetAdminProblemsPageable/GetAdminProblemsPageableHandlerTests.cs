using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Enums;
using GetAdminProblemsPageableHandler = StackDuel.Application.Queries.Problems.GetAdminProblemsPageable.GetAdminProblemsPageableHandler;
using GetAdminProblemsPageableQuery = StackDuel.Application.Queries.Problems.GetAdminProblemsPageable.GetAdminProblemsPageableQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetAdminProblemsPageable;

public class GetAdminProblemsPageableHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private GetAdminProblemsPageableHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetAdminProblemsPageableHandler(_problemReadRepository.Object, _languageReadRepository.Object);
    }

    private static AdminProblemListRowDto CreateRow(int difficultyValue, IReadOnlyList<Guid>? versionIds = null) =>
        new(
            Guid.NewGuid(),
            "two-sum",
            "Two Sum",
            difficultyValue,
            ProblemStatus.Published,
            1000,
            256,
            ["arrays"],
            versionIds ?? [],
            1,
            DateTime.UtcNow,
            "alice"
        );

    [Test]
    public async Task Handle_PassesThroughPageMetadata()
    {
        var page = new PageResult<AdminProblemListRowDto>
        {
            Results = [CreateRow(100)],
            Total = 42,
            Page = 2,
            Size = 10,
        };
        _problemReadRepository
            .Setup(x =>
                x.GetAdminProblemsPagedAsync(
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(page);
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var pagination = new PaginationRequest { Page = 2, Size = 10 };
        var query = new GetAdminProblemsPageableQuery(pagination, "two");
        Result<PageResult<AdminProblemListItemDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Total, Is.EqualTo(42));
            Assert.That(result.Value.Page, Is.EqualTo(2));
            Assert.That(result.Value.Size, Is.EqualTo(10));
            Assert.That(result.Value.Results, Has.Count.EqualTo(1));
        });
        _problemReadRepository.Verify(
            x => x.GetAdminProblemsPagedAsync(pagination, "two", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_KnownLanguageVersion_MapsDisplayName()
    {
        var language = new Language(new LanguageName("Python"), new LanguageSlug("python"), Guid.NewGuid());
        var versionEntry = language.AddVersion(new LanguageVersion("3.12"), new Judge0Id(71));
        _problemReadRepository
            .Setup(x =>
                x.GetAdminProblemsPagedAsync(
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new PageResult<AdminProblemListRowDto>
                {
                    Results = [CreateRow(100, [versionEntry.Id])],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        var query = new GetAdminProblemsPageableQuery(new PaginationRequest { Page = 1, Size = 20 }, null);
        Result<PageResult<AdminProblemListItemDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Value.Results[0].Languages, Is.EquivalentTo(new[] { "Python 3.12" }));
    }

    [Test]
    public async Task Handle_UnknownLanguageVersion_FallsBackToUnknown()
    {
        _problemReadRepository
            .Setup(x =>
                x.GetAdminProblemsPagedAsync(
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new PageResult<AdminProblemListRowDto>
                {
                    Results = [CreateRow(100, [Guid.NewGuid()])],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new GetAdminProblemsPageableQuery(new PaginationRequest { Page = 1, Size = 20 }, null);
        Result<PageResult<AdminProblemListItemDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Value.Results[0].Languages, Is.EquivalentTo(new[] { "Unknown" }));
    }

    [TestCase(0, DifficultyTier.Beginner)]
    [TestCase(200, DifficultyTier.Beginner)]
    [TestCase(201, DifficultyTier.Easy)]
    [TestCase(500, DifficultyTier.Easy)]
    [TestCase(501, DifficultyTier.Intermediate)]
    [TestCase(1000, DifficultyTier.Intermediate)]
    [TestCase(1001, DifficultyTier.Advanced)]
    [TestCase(2000, DifficultyTier.Advanced)]
    [TestCase(2001, DifficultyTier.Expert)]
    public async Task Handle_MapsDifficultyValueToExpectedTier(int difficultyValue, DifficultyTier expectedTier)
    {
        _problemReadRepository
            .Setup(x =>
                x.GetAdminProblemsPagedAsync(
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new PageResult<AdminProblemListRowDto>
                {
                    Results = [CreateRow(difficultyValue)],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new GetAdminProblemsPageableQuery(new PaginationRequest { Page = 1, Size = 20 }, null);
        Result<PageResult<AdminProblemListItemDto>> result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result.Value.Results[0].DifficultyTier, Is.EqualTo(expectedTier));
    }
}