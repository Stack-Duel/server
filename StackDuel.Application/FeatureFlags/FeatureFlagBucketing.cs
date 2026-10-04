using System.Text;

namespace StackDuel.Application.FeatureFlags;

public static class FeatureFlagBucketing
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    public static int GetBucket(string flagKey, Guid userId)
    {
        ulong hash = FnvOffsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes($"{flagKey}:{userId:D}"))
        {
            hash ^= b;
            hash *= FnvPrime;
        }

        return (int)(hash % 100);
    }

    public static bool IsInRollout(string flagKey, Guid userId, int rolloutPercentage)
    {
        if (rolloutPercentage <= 0)
            return false;
        if (rolloutPercentage >= 100)
            return true;

        return GetBucket(flagKey, userId) < rolloutPercentage;
    }
}