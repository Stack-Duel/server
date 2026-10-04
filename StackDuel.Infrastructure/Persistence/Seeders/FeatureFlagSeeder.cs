using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class FeatureFlagSeeder(StackDuelDbContext context) : IStaticSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        FeatureFlagKey leaderboardsKey = new(WellKnownFeatureFlags.Leaderboards);

        FeatureFlag leaderboardsFlag =
            await context.FeatureFlags.FirstOrDefaultAsync(f => f.Key == leaderboardsKey, cancellationToken)
            ?? FeatureFlag.Create(
                leaderboardsKey,
                "Leaderboards",
                "Global kill-switch for the leaderboards page and endpoint.",
                defaultEnabled: true
            );

        if (context.Entry(leaderboardsFlag).State == EntityState.Detached)
            await context.FeatureFlags.AddAsync(leaderboardsFlag, cancellationToken);

        FeatureFlagKey campaignsKey = new(WellKnownFeatureFlags.Campaigns);

        FeatureFlag campaignsFlag =
            await context.FeatureFlags.FirstOrDefaultAsync(f => f.Key == campaignsKey, cancellationToken)
            ?? FeatureFlag.Create(
                campaignsKey,
                "Campaigns",
                "Global kill-switch for the campaigns/learning-path feature.",
                defaultEnabled: false
            );

        if (context.Entry(campaignsFlag).State == EntityState.Detached)
            await context.FeatureFlags.AddAsync(campaignsFlag, cancellationToken);

        FeatureFlagKey ratingsKey = new(WellKnownFeatureFlags.Ratings);

        FeatureFlag ratingsFlag =
            await context.FeatureFlags.FirstOrDefaultAsync(f => f.Key == ratingsKey, cancellationToken)
            ?? FeatureFlag.Create(
                ratingsKey,
                "Ratings",
                "Kill-switch for the player ranking UI in profiles; the duel/ffa/solo-rush mode-rating backend is currently removed.",
                defaultEnabled: false
            );

        if (context.Entry(ratingsFlag).State == EntityState.Detached)
            await context.FeatureFlags.AddAsync(ratingsFlag, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}