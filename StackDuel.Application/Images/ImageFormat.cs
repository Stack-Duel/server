namespace StackDuel.Application.Images;

public sealed record ImageFormat(string ContentType, string FileExtension, Func<byte[], bool> Matches);