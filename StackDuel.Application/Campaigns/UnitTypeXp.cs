using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Campaigns;

internal static class UnitTypeXp
{
    public static int For(UnitType unitType) =>
        unitType switch
        {
            UnitType.Lesson => 10,
            UnitType.Quiz => 15,
            UnitType.Challenge => 25,
            _ => 10,
        };
}