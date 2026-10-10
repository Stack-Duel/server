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

    public GetAdminProblemsPageableHandlerTests()
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

    [Fact]
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

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.Total);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(10, result.Value.Size);
        Assert.Single(result.Value.Results);
        _problemReadRepository.Verify(
            x => x.GetAdminProblemsPagedAsync(pagination, "two", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
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

        Assert.Equivalent(new[] { "Python 3.12" }, result.Value.Results[0].Languages, strict: true);
    }

    [Fact]
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

        Assert.Equivalent(new[] { "Unknown" }, result.Value.Results[0].Languages, strict: true);
    }

    [Theory]
    [InlineData(0, DifficultyTier.Beginner)]
    [InlineData(200, DifficultyTier.Beginner)]
    [InlineData(201, DifficultyTier.Easy)]
    [InlineData(500, DifficultyTier.Easy)]
    [InlineData(501, DifficultyTier.Intermediate)]
    [InlineData(1000, DifficultyTier.Intermediate)]
    [InlineData(1001, DifficultyTier.Advanced)]
    [InlineData(2000, DifficultyTier.Advanced)]
    [InlineData(2001, DifficultyTier.Expert)]
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

        Assert.Equal(expectedTier, result.Value.Results[0].DifficultyTier);
    }
}