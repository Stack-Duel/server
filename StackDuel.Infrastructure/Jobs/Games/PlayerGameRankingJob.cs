using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Quartz;

namespace StackDuel.Infrastructure.Jobs.Games;



internal sealed partial class PlayerGameRankingJob : IJob
{
    public static readonly JobKey Key = new(nameof(PlayerGameRankingJob), "Games");


    // TODO: Implement this job
    private readonly StackDuelDbContext _db;
    private readonly IPlayerRatingService _playerRating;
    private readonly IProblemRatingService _problemRating;;
    private readonly ISeasonService _seasons;

    public PlayerGameRankingJob(
        StackDuelDbContext db, 
        IPlayerRatingService playerRating, 
        IProblemRatingService problemRating, 
        ISeasonService seasons)
    {
        _db = db;
        _playerRating = playerRating;
        _problemRating = problemRating;
        _seasons = seasons;
    }

    public Task Execute(IJobExecutionContext context)
    {
        var gameId = context.MergedJobDataMap.GetInt("gameId");
        LogExecuting(gameId);

        try{
            var result = await _playerRatings.ApplyGameRatingsAsync(gameId, context.CancellationToken);
            LogResult(gameId, result.Status, result.PlayersUpdated, result.ProblemsUpdated);
            
        }
        
        catch (OperationCanceledException)
        {
            LogCancelled(gameId);
            throw;
        }

        catch (Exception ex)
        {
            LogFailed(gameId, ex);
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing PlayerGameRankingJob")]
    private partial void LogExecuting(int gameId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "PlayerGameRankingJob finished with status {Status}, updated {PlayersUpdated} players and {ProblemsUpdated} problems")]
}
