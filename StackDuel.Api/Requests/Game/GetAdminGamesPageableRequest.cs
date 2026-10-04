using StackDuel.Domain.Games.Enums;

namespace StackDuel.Api.Requests.Game;

public sealed record GetAdminGamesPageableRequest(int Page, int Size, DateTime Timestamp, GameStatus? Status);