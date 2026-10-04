using StackDuel.Application.LanguageServer;

namespace StackDuel.Api.Responses.LanguageServer;

public sealed record LanguageServerSessionResponse(
    Guid SessionId,
    string RootUri,
    string DocumentUri,
    string LanguageId,
    object? InitializationOptions
)
{
    public static LanguageServerSessionResponse FromSession(LanguageServerSession session) =>
        new(session.Id, session.RootUri, session.DocumentUri, session.LanguageSlug, session.InitializationOptions);
}