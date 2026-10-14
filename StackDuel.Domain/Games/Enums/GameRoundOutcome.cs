namespace StackDuel.Domain.Games.Enums;

/// <summary>
/// How a participant's attempt at a round ended. There is deliberately no "in progress" member — a
/// round is in progress precisely when it has no completion recorded against it, so the absence of
/// a <c>game_round_completions</c> row carries that state instead of an enum value that would force
/// every other column on the row to be nullable.
/// </summary>
/// <remarks>
/// New game modes add members here. An outcome that needs no data of its own costs nothing but a
/// member; one that does gets its own table keyed on (round_participant_id, outcome), the way
/// <c>game_round_solutions</c> does for <see cref="Solved"/>.
/// </remarks>
public enum GameRoundOutcome
{
    Solved = 0,

    Skipped = 1,
}