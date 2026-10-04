namespace StackDuel.Api;

internal static class LoggingEventIds
{
    internal static class Accounts
    {
        public const int ContextMissingSub = 1000;
        public const int ContextResolveFailed = 1001;
        public const int ContextPublicEndpoint = 1002;
    }

    internal static class Exceptions
    {
        public const int UnhandledException = 5000;
        public const int UnhandledExceptionWithPath = 5001;
    }
}