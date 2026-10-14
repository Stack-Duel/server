using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Games;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Games;

internal sealed class GameRoundSubmissionReadRepository(StackDuelDbContext context)
    : IGameRoundSubmissionReadRepository
{
    public async Task<bool> IsLatestAttemptAsync(
        Guid participantId,
        Guid roundId,
        Guid submissionId,
        CancellationToken cancellationToken = default
    )
    {
        Guid latestSubmissionId = await context
            .GameRoundSubmissions.Where(grs => grs.ParticipantId == participantId && grs.RoundId == roundId)
            .OrderByDescending(grs => grs.CreatedAt)
            .Select(grs => grs.SubmissionId)
            .FirstOrDefaultAsync(cancellationToken);

        return latestSubmissionId != Guid.Empty && latestSubmissionId == submissionId;
    }
}