namespace StackDuel.Api.RateLimiting;

public static class WellKnownPolicies
{
    public const string General = "rl:general";

    public const string Submissions = "rl:submissions";

    public const string AvatarUpload = "rl:avatar-upload";

    public const string Judge0Callback = "rl:judge0-callback";
}