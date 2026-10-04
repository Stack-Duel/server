using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class ProblemReactionTypeSeeder(StackDuelDbContext context) : IStaticSeeder
{
    private const string LikeKey = "like";
    private const string DislikeKey = "dislike";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLikeAsync(cancellationToken);
        await EnsureDislikeAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLikeAsync(CancellationToken cancellationToken)
    {
        bool exists = await context.ProblemReactionTypes.AnyAsync(type => type.Key == LikeKey, cancellationToken);

        if (exists)
            return;

        ProblemReactionType like = new(LikeKey, "Like", "\U0001F44D", sortOrder: 1);

        await context.ProblemReactionTypes.AddAsync(like, cancellationToken);
    }

    private async Task EnsureDislikeAsync(CancellationToken cancellationToken)
    {
        bool exists = await context.ProblemReactionTypes.AnyAsync(type => type.Key == DislikeKey, cancellationToken);

        if (exists)
            return;

        ProblemReactionType dislike = new(DislikeKey, "Dislike", "\U0001F44E", sortOrder: 2);

        await context.ProblemReactionTypes.AddAsync(dislike, cancellationToken);
    }
}