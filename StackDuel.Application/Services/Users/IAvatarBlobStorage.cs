namespace StackDuel.Application.Services.Users;

public interface IAvatarBlobStorage
{
    Task<string> UploadAsync(
        Guid userId,
        Guid avatarId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}