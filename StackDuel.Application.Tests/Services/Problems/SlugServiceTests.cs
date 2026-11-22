using Moq;
using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.ValueObjects;
using SlugService = StackDuel.Application.Services.Problems.SlugService;

namespace StackDuel.Application.Tests.Services.Problems;

public class SlugServiceTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly SlugService _slugService;

    public SlugServiceTests()
    {
        _slugService = new SlugService(_problemReadRepository.Object);

        _problemReadRepository
            .Setup(repository => repository.SlugExistsAsync(It.IsAny<Slug>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetUpTakenSlugs(params string[] taken) =>
        _problemReadRepository
            .Setup(repository => repository.SlugExistsAsync(It.IsAny<Slug>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Slug slug, CancellationToken _) => taken.Contains(slug.Value));

    [Fact]
    public async Task AFreeTitleKeepsItsPlainSlug()
    {
        Slug slug = await _slugService.ResolveUniqueSlugFromTitleAsync(new Title("Count Repeated Values"));

        Assert.Equal("count-repeated-values", slug.Value);
    }

    [Fact]
    public async Task ATakenSlugGetsASuffix()
    {
        SetUpTakenSlugs("count-vowels");

        Slug slug = await _slugService.ResolveUniqueSlugFromTitleAsync(new Title("Count Vowels"));

        Assert.Equal("count-vowels-2", slug.Value);
    }

    [Fact]
    public async Task SuffixesKeepClimbingUntilOneIsFree()
    {
        SetUpTakenSlugs("count-vowels", "count-vowels-2", "count-vowels-3");

        Slug slug = await _slugService.ResolveUniqueSlugFromTitleAsync(new Title("Count Vowels"));

        Assert.Equal("count-vowels-4", slug.Value);
    }

    [Fact]
    public void ADraftSlugIsRecognisableAsOne()
    {
        Assert.True(_slugService.GenerateDraftSlug().IsDraftSlug);
    }

    [Fact]
    public void ATitleSlugIsNotADraftSlug()
    {
        Assert.False(_slugService.GenerateSlugFromTitle(new Title("Count Vowels")).IsDraftSlug);
    }
}