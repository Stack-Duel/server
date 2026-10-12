
namespace StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

internal sealed partial class CalculatePlayerRankingsFromGameHandler() : AbstractCommandHandler<CalculatePlayerRankingsFromGameCommand, CalculatePlayerRankingsFromGameResult>
{
    protected override async Task<Result<CalculatePlayerRankingsFromGameResult>> HandleValidated(
        CalculatePlayerRankingsFromGameCommand request,
        CancellationToken cancellationToken
    )
    {
        // validate that the game is actually over and that the player rankings can be calculated, and haven't already been calculated.
        
        // Implement the logic to calculate player rankings from the game here.
        
    }
}