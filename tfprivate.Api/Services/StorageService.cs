using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace tfprivate.Api.Services;

public record ModuleInfo(
    string Name,
    string Version,
    string Description,
    string Source,
    DateTimeOffset PublishedAt,
    Uri DownloadUrl);

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
    Task ValidateConnectionAsync();
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

    public async Task ValidateConnectionAsync()
    {
        try
        {
            // Try to get account info which will validate the credentials
            await _blobServiceClient.GetAccountInfoAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to validate Azure Storage connection. Please check your credentials and connection string.", ex);
        }
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
        await container.CreateIfNotExistsAsync();

        var modules = new List<ModuleInfo>();
        var moduleVersions = new Dictionary<string, ModuleInfo>();

        try
        {
            await foreach (var blob in container.GetBlobsAsync(prefix: $"{org}/"))
            {
                if (blob.Name.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse module info from blob path
                    // Expected format: org/module_name/v1.0.0/module.tgz
                    var parts = blob.Name.Split('/');
                    if (parts.Length >= 4)  // Changed from 5 to 4 since we removed provider
                    {
                        var moduleName = parts[1];
                        var version = parts[2].TrimStart('v');  // Remove 'v' prefix if present

                        // Get blob properties to access metadata and creation time
                        var blobClient = container.GetBlobClient(blob.Name);
                        var properties = await blobClient.GetPropertiesAsync();

                        string description = "", source = "";

                        properties.Value.Metadata.TryGetValue("description", out var descVal);
                        description = descVal ?? "";

                        properties.Value.Metadata.TryGetValue("source", out var sourceVal);
                        source = sourceVal ?? "";

                        var publishedAt = properties.Value.CreatedOn;
                        var downloadUrl = await GetDownloadUrlAsync(containerName, blob.Name, TimeSpan.FromMinutes(5));

                        var moduleInfo = new ModuleInfo(
                            moduleName,
                            version,
                            description,
                            source,
                            publishedAt,
                            downloadUrl
                        );

                        // Keep only the latest version of each module
                        var key = moduleName;
                        if (!moduleVersions.ContainsKey(key) ||
                            SemanticVersion.Parse(version) > SemanticVersion.Parse(moduleVersions[key].Version))
                        {
                            moduleVersions[key] = moduleInfo;
                        }
                    }
                }
            }

            modules.AddRange(moduleVersions.Values);
        }
        catch (Exception)
        {
            throw;
        }

        return modules;
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
