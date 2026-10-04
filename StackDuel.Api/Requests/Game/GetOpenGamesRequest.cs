namespace StackDuel.Api.Requests.Game;

public sealed record GetOpenGamesRequest(string? GameModeKey, int Page, int Size);