using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using GetProblemsPageableHandler = StackDuel.Application.Queries.Problems.GetProblemsPageable.GetProblemsPageableHandler;
using GetProblemsPageableQuery = StackDuel.Application.Queries.Problems.GetProblemsPageable.GetProblemsPageableQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetProblemsPageable;

public class GetProblemsPageableHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private GetProblemsPageableHandler _handler = null!;

    public GetProblemsPageableHandlerTests()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetProblemsPageableHandler(_problemReadRepository.Object, _languageReadRepository.Object);
    }

    private static ProblemListRowDto CreateRow(int difficultyValue = 100, IReadOnlyList<Guid>? versionIds = null) =>
        new(Guid.NewGuid(), "two-sum", "Two Sum", difficultyValue, ["arrays"], versionIds ?? []);

    [Fact]
    public async Task Handle_PassesThroughPageMetadata()
    {
        var page = new PageResult<ProblemListRowDto>
        {
            Results = [CreateRow()],
            Total = 1,
            Page = 1,
            Size = 20,
        };
        var pagination = new PaginationRequest { Page = 1, Size = 20 };
        _problemReadRepository
            .Setup(x => x.GetPagedAsync(pagination, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetProblemsPageableQuery(pagination, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.Size);
        Assert.Single(result.Value.Results);
    }

    [Fact]
    public async Task Handle_PassesPaginationRequestAndSearchThroughToRepository()
    {
        var pagination = new PaginationRequest { Page = 3, Size = 5 };
        _problemReadRepository
            .Setup(x =>
                x.GetPagedAsync(It.IsAny<PaginationRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new PageResult<ProblemListRowDto>
                {
                    Results = [],
                    Total = 0,
                    Page = 3,
                    Size = 5,
                }
            );
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _handler.Handle(new GetProblemsPageableQuery(pagination, "two sum"), CancellationToken.None);

        _problemReadRepository.Verify(
            x => x.GetPagedAsync(pagination, "two sum", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_KnownLanguageVersion_MapsLanguageWithSlug()
    {
        var language = new Language(new LanguageName("SQLite"), new LanguageSlug("sqlite"), Guid.NewGuid());
        var versionEntry = language.AddVersion(new LanguageVersion("3.27.2"), new Judge0Id(82));
        _problemReadRepository
            .Setup(x =>
                x.GetPagedAsync(It.IsAny<PaginationRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new PageResult<ProblemListRowDto>
                {
                    Results = [CreateRow(versionIds: [versionEntry.Id])],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        var query = new GetProblemsPageableQuery(new PaginationRequest { Page = 1, Size = 20 }, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Value.Results[0].Languages);
        Assert.Equal("SQLite", result.Value.Results[0].Languages[0].Name);
        Assert.Equal("sqlite", result.Value.Results[0].Languages[0].Slug);
    }

    [Fact]
    public async Task Handle_MultipleVersionsOfSameLanguage_DeduplicatesToOneEntry()
    {
        var language = new Language(new LanguageName("Python"), new LanguageSlug("python"), Guid.NewGuid());
        var v1 = language.AddVersion(new LanguageVersion("3.12"), new Judge0Id(71));
        var v2 = language.AddVersion(new LanguageVersion("3.13"), new Judge0Id(109));
        _problemReadRepository
            .Setup(x =>
                x.GetPagedAsync(It.IsAny<PaginationRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new PageResult<ProblemListRowDto>
                {
                    Results = [CreateRow(versionIds: [v1.Id, v2.Id])],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        var query = new GetProblemsPageableQuery(new PaginationRequest { Page = 1, Size = 20 }, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Value.Results[0].Languages);
    }

    [Fact]
    public async Task Handle_UnknownLanguageVersion_OmitsFromLanguages()
    {
        _problemReadRepository
            .Setup(x =>
                x.GetPagedAsync(It.IsAny<PaginationRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new PageResult<ProblemListRowDto>
                {
                    Results = [CreateRow(versionIds: [Guid.NewGuid()])],
                    Total = 1,
                    Page = 1,
                    Size = 20,
                }
            );
        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new GetProblemsPageableQuery(new PaginationRequest { Page = 1, Size = 20 }, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result.Value.Results[0].Languages);
    }
}