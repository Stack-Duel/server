using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Enums;
using GetMyProblemsHandler = StackDuel.Application.Queries.Problems.GetMyProblems.GetMyProblemsHandler;
using GetMyProblemsQuery = StackDuel.Application.Queries.Problems.GetMyProblems.GetMyProblemsQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetMyProblems;

public class GetMyProblemsHandlerTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly Mock<ILanguageReadRepository> _languageReadRepository = new();
    private readonly UserContext _userContext = new() { User = CreateUserDto() };
    private readonly GetMyProblemsHandler _handler;
    private static readonly string[] expectedTags = ["arrays"];
    private static readonly string[] expectedLanguages = ["JavaScript"];

    public GetMyProblemsHandlerTests()
    {
        _handler = new GetMyProblemsHandler(
            _problemReadRepository.Object,
            _languageReadRepository.Object,
            _userContext
        );

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static UserDto CreateUserDto() =>
        new(Guid.NewGuid(), "auth0|abc", "alice", null, null, false, null, DateTime.UtcNow, null, []);

    private static GetMyProblemsQuery CreateQuery(
        IReadOnlyCollection<ProblemStatus>? statuses = null,
        string? search = null
    ) => new(new GetMyProblemsInput(new PaginationRequest { Page = 1, Size = 10 }, statuses ?? [], search));

    private static PageResult<AdminProblemListRowDto> CreatePage(params AdminProblemListRowDto[] rows) =>
        new()
        {
            Results = rows,
            Total = rows.Length,
            Page = 1,
            Size = 10,
        };

    private static AdminProblemListRowDto CreateRow(
        Guid? id = null,
        ProblemStatus status = ProblemStatus.Draft,
        IReadOnlyList<Guid>? languageVersionIds = null
    ) =>
        new(
            id ?? Guid.NewGuid(),
            "two-sum",
            "Two Sum",
            320,
            status,
            1000,
            256,
            ["arrays"],
            languageVersionIds ?? [],
            1,
            DateTime.UtcNow,
            "alice"
        );

    [Fact]
    public async Task Handle_ScopesToCurrentUserAndPassesFiltersThrough()
    {
        _problemReadRepository
            .Setup(x =>
                x.GetMyProblemsPagedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<ProblemStatus>>(),
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(CreatePage());

        ProblemStatus[] statuses = [ProblemStatus.Draft, ProblemStatus.Pending];
        await _handler.Handle(CreateQuery(statuses, "two"), CancellationToken.None);

        _problemReadRepository.Verify(
            x =>
                x.GetMyProblemsPagedAsync(
                    _userContext.User!.Id,
                    statuses,
                    It.Is<PaginationRequest>(p => p.Page == 1 && p.Size == 10),
                    "two",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_MapsRowFieldsAndComputesDifficultyTier()
    {
        AdminProblemListRowDto row = CreateRow(status: ProblemStatus.Published);
        _problemReadRepository
            .Setup(x =>
                x.GetMyProblemsPagedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<ProblemStatus>>(),
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(CreatePage(row));

        Result<PageResult<MyProblemListItemDto>> result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        MyProblemListItemDto dto = Assert.Single(result.Value.Results);
        Assert.Equal(row.Id, dto.Id);
        Assert.Equal("two-sum", dto.Slug);
        Assert.Equal("Two Sum", dto.Title);
        Assert.Equal(ProblemStatus.Published, dto.Status);
        Assert.Equal(DifficultyTier.Easy, dto.DifficultyTier);
        Assert.Equivalent(expectedTags, dto.Tags, strict: true);
    }

    [Fact]
    public async Task Handle_MultipleSetupsOnSameLanguage_CollapsesLanguagesToOneEntry()
    {
        var language = new Language(new LanguageName("JavaScript"), new LanguageSlug("javascript"), Guid.NewGuid());
        LanguageVersionEntry version18 = language.AddVersion(new LanguageVersion("18"), new Judge0Id(63));
        LanguageVersionEntry version20 = language.AddVersion(new LanguageVersion("20"), new Judge0Id(93));

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        AdminProblemListRowDto row = CreateRow(languageVersionIds: [version18.Id, version20.Id]);
        _problemReadRepository
            .Setup(x =>
                x.GetMyProblemsPagedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<ProblemStatus>>(),
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(CreatePage(row));

        Result<PageResult<MyProblemListItemDto>> result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        MyProblemListItemDto dto = Assert.Single(result.Value.Results);
        Assert.Equivalent(expectedLanguages, dto.Languages, strict: true);
    }

    [Fact]
    public async Task Handle_NoResults_ReturnsEmptyPage()
    {
        _problemReadRepository
            .Setup(x =>
                x.GetMyProblemsPagedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<ProblemStatus>>(),
                    It.IsAny<PaginationRequest>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(CreatePage());

        Result<PageResult<MyProblemListItemDto>> result = await _handler.Handle(CreateQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Results);
    }
}
