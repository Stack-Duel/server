using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class GameModeSeeder(StackDuelDbContext context) : IStaticSeeder
{
    private const string SoloRushKey = "solo_rush";
    private const string DuelKey = "duel";
    private const string FfaKey = "ffa";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Guid defaultPoolId = await context
            .ProblemPools.Where(p => p.Key == ProblemPoolSeeder.AllProblemsKey)
            .Select(p => p.Id)
            .SingleAsync(cancellationToken);

        await EnsureSoloRushAsync(defaultPoolId, cancellationToken);
        await EnsureDuelAsync(defaultPoolId, cancellationToken);
        await EnsureFfaAsync(defaultPoolId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSoloRushAsync(Guid defaultPoolId, CancellationToken cancellationToken)
    {
        bool exists = await context.GameModes.AnyAsync(m => m.Key == SoloRushKey, cancellationToken);

        if (exists)
            return;

        GameMode soloRush = new(
            SoloRushKey,
            "Solo Rush",
            "Race against the clock solving algorithmic problems solo. "
                + "Problems start easy and get progressively harder as you go.",
            isBuiltIn: true,
            minPlayers: 1,
            maxPlayers: 1,
            defaultPoolId
        );

        soloRush.AddTimeOption(300, isDefault: false); // 5 min
        soloRush.AddTimeOption(600, isDefault: true); // 10 min (default)
        soloRush.AddTimeOption(900, isDefault: false); // 15 min
        soloRush.AddTimeOption(1800, isDefault: false); // 30 min

        await context.GameModes.AddAsync(soloRush, cancellationToken);
    }

    private async Task EnsureDuelAsync(Guid defaultPoolId, CancellationToken cancellationToken)
    {
        bool exists = await context.GameModes.AnyAsync(m => m.Key == DuelKey, cancellationToken);

        if (exists)
            return;

        GameMode duel = new(
            DuelKey,
            "Duel",
            "Challenge your opponent in a one-on-one, head-to-head battle.",
            isBuiltIn: true,
            minPlayers: 2,
            maxPlayers: 2,
            defaultPoolId
        );

        duel.AddTimeOption(300, isDefault: false); // 5 min
        duel.AddTimeOption(600, isDefault: true); // 10 min (default)
        duel.AddTimeOption(900, isDefault: false); // 15 min

        await context.GameModes.AddAsync(duel, cancellationToken);
    }

    private async Task EnsureFfaAsync(Guid defaultPoolId, CancellationToken cancellationToken)
    {
        bool exists = await context.GameModes.AnyAsync(m => m.Key == FfaKey, cancellationToken);

        if (exists)
            return;

        GameMode ffa = new(
            FfaKey,
            "FFA",
            "Free-for-all: solve as many problems as possible before the time runs out.",
            isBuiltIn: true,
            minPlayers: 2,
            maxPlayers: 10,
            defaultPoolId
        );

        ffa.AddTimeOption(300, isDefault: false); // 5 min
        ffa.AddTimeOption(600, isDefault: true); // 10 min (default)
        ffa.AddTimeOption(900, isDefault: false); // 15 min
        ffa.AddTimeOption(1800, isDefault: false); // 30 min

        await context.GameModes.AddAsync(ffa, cancellationToken);
    }
}