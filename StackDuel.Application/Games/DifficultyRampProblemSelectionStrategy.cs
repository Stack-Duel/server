using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.ValueObjects;
using System.Security.Cryptography;

namespace StackDuel.Application.Games;

internal sealed class DifficultyRampProblemSelectionStrategy(
    string gameModeKey,
    IProblemReadRepository problemReadRepository
) : IProblemSelectionStrategy
{
    private static readonly DifficultyBand[] Bands =
    [
        new(0, 2, Difficulty.BeginnerMin, 120),
        new(3, 5, 121, Difficulty.BeginnerMax),
        new(6, 9, Difficulty.EasyMin, 350),
        new(10, int.MaxValue, 351, Difficulty.EasyMax),
    ];

    public string GameModeKey { get; } = gameModeKey;

    public async Task<Guid?> SelectNextProblemIdAsync(
        ProblemSelectionContext context,
        CancellationToken cancellationToken = default
    )
    {
        DifficultyBand primaryBand = Bands.First(band =>
            context.RoundIndex >= band.FromRound && context.RoundIndex <= band.ToRound
        );

        List<DifficultyBand> orderedBands =
        [
            primaryBand,
            .. Bands.Where(b => b != primaryBand).OrderBy(b => Math.Abs(b.FromRound - context.RoundIndex)),
        ];

        foreach (DifficultyBand band in orderedBands)
        {
            long selectionSeed = ComputeSelectionSeed(context.GameId, context.RoundIndex, band);

            Guid? problemId = await problemReadRepository.GetRandomProblemIdByDifficultyAsync(
                context.PoolId,
                band.MinDifficulty,
                band.MaxDifficulty,
                context.ExcludedProblemIds,
                selectionSeed,
                context.AllowedLanguageVersionIds,
                cancellationToken
            );

            if (problemId.HasValue)
                return problemId.Value;
        }

        return null;
    }

    private static long ComputeSelectionSeed(Guid gameId, int roundIndex, DifficultyBand band)
    {
        Span<byte> buffer = stackalloc byte[28];
        gameId.TryWriteBytes(buffer);
        BitConverter.TryWriteBytes(buffer[16..], roundIndex);
        BitConverter.TryWriteBytes(buffer[20..], band.MinDifficulty);
        BitConverter.TryWriteBytes(buffer[24..], band.MaxDifficulty);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(buffer, hash);
        return BitConverter.ToInt64(hash);
    }

    private sealed record DifficultyBand(int FromRound, int ToRound, int MinDifficulty, int MaxDifficulty);
}