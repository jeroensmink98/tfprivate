using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System.Text.RegularExpressions;

namespace tfprivate.Api.Services;

public record ModuleInfo(string Name, Uri DownloadUrl);

public interface IStorageService
{
    Task<Uri> GetUploadUrlAsync(string containerName, string blobName, TimeSpan? validFor = null);
    Task<Uri> GetDownloadUrlAsync(string containerName, string blobName, TimeSpan? validFor = null);
    Task DeleteBlobAsync(string containerName, string blobName);
    Task<IEnumerable<string>> ListVersionsAsync(string containerName, string prefix);
    Task<IEnumerable<ModuleInfo>> ListModulesAsync(string containerName, string org);
    Task UploadFromStreamAsync(string containerName, string blobName, Stream content);
    Task UploadFromStreamAsync(string containerName, string blobName, Stream content, IDictionary<string, string>? metadata);
    Task SetBlobMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata);
}

public class StorageService : IStorageService
{
    private readonly string _connectionString;
    private readonly BlobServiceClient _blobServiceClient;

    public StorageService(IConfiguration configuration)
    {
        // Try to get connection string from environment variables first
        var accountName = Environment.GetEnvironmentVariable("STORAGE_ACCOUNTNAME");
        var accountKey = Environment.GetEnvironmentVariable("STORAGE_ACCESS_KEY");

        if (!string.IsNullOrEmpty(accountName) && !string.IsNullOrEmpty(accountKey))
        {
            _connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";
        }
        else
        {
            _connectionString = configuration["Azure:Storage:ConnectionString"]
                ?? throw new ArgumentNullException("Storage connection string not configured");
        }

        _blobServiceClient = new BlobServiceClient(_connectionString);
    }

    public async Task<IEnumerable<string>> ListVersionsAsync(string containerName, string prefix)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var versions = new HashSet<string>();
        var versionPattern = new Regex(@"v(\d+\.\d+\.\d+)/module\.tgz$");

        await foreach (var blob in container.GetBlobsAsync(prefix: prefix))
        {
            var match = versionPattern.Match(blob.Name);
            if (match.Success)
            {
                versions.Add(match.Groups[1].Value);
            }
        }

        return versions.OrderByDescending(v => v);
    }

    public async Task<Uri> GetUploadUrlAsync(string containerName, string blobName, TimeSpan? validFor = null)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();

        var blobClient = container.GetBlobClient(blobName);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.Add(validFor ?? TimeSpan.FromMinutes(15))
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri;
    }

    public async Task<Uri> GetDownloadUrlAsync(string containerName, string blobName, TimeSpan? validFor = null)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);

        // Check if blob exists
        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob {blobName} not found in container {containerName}");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.Add(validFor ?? TimeSpan.FromMinutes(15))
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri;
    }

    public async Task DeleteBlobAsync(string containerName, string blobName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<IEnumerable<ModuleInfo>> ListModulesAsync(string containerName, string org)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);

        // Create container if it doesn't exist
        await container.CreateIfNotExistsAsync();

        var modules = new List<ModuleInfo>();

        try
        {
            await foreach (var blob in container.GetBlobsAsync(prefix: $"{org}/"))
            {
                if (blob.Name.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
                {
                    var downloadUrl = await GetDownloadUrlAsync(containerName, blob.Name, TimeSpan.FromMinutes(5));
                    modules.Add(new ModuleInfo(blob.Name, downloadUrl));
                }
            }

            return modules.OrderBy(m => m.Name);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list modules in container {containerName}: {ex.Message}", ex);
        }
    }

    public async Task UploadFromStreamAsync(string containerName, string blobName, Stream content)
    {
        await UploadFromStreamAsync(containerName, blobName, content, null);
    }

    public async Task UploadFromStreamAsync(string containerName, string blobName, Stream content, IDictionary<string, string>? metadata)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();

        var blobClient = container.GetBlobClient(blobName);

        var options = new BlobUploadOptions();
        if (metadata != null)
        {
            options.Metadata = metadata;
        }

        await blobClient.UploadAsync(content, options);
    }

    public async Task SetBlobMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.SetMetadataAsync(metadata);
    }
}
