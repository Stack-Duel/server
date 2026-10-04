using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using StackDuel.Application.Configuration;
using StackDuel.Application.Images;
using StackDuel.Application.Services.Users;

namespace StackDuel.Infrastructure.Storage;

internal sealed class AzureAvatarBlobStorage(BlobServiceClient blobServiceClient, AzureStorageOptions options)
    : IAvatarBlobStorage
{
    public async Task<string> UploadAsync(
        Guid userId,
        Guid avatarId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(options.AvatarContainerName);

        if (options.AutoCreateContainer)
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        string blobName = $"{userId}/{avatarId}{ImageSignature.ExtensionFor(contentType)}";
        BlobClient blob = container.GetBlobClient(blobName);

        using var stream = new MemoryStream(content);
        await blob.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken
        );

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var uriBuilder = new BlobUriBuilder(new Uri(blobUrl));
        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(uriBuilder.BlobContainerName);
        await container.GetBlobClient(uriBuilder.BlobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}