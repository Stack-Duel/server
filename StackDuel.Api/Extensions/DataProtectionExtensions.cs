using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using StackDuel.Application.Configuration;

namespace StackDuel.Api.Extensions;

public static class DataProtectionExtensions
{
    private const string KeyContainerName = "data-protection-keys";
    private const string KeyBlobName = "keys.xml";

    public static IServiceCollection AddPersistedDataProtection(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var storageOptions = configuration.GetSection(AzureStorageOptions.SectionName).Get<AzureStorageOptions>();
        string? connectionString = storageOptions?.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        var keyContainer = new BlobContainerClient(connectionString, KeyContainerName);
        keyContainer.CreateIfNotExists();

        services.AddDataProtection().PersistKeysToAzureBlobStorage(keyContainer.GetBlobClient(KeyBlobName));

        return services;
    }
}