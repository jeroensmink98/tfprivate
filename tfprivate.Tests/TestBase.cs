using System.Net.Http.Headers;
using tfprivate.Tests.Helpers;

namespace tfprivate.Tests;

public class TestBase : IDisposable
{
    protected readonly HttpClient _client;
    protected readonly string TestApiKey;
    protected readonly string TestNamespace;
    private readonly string ApiBaseUrl;

    static TestBase()
    {
        // Load environment variables before any tests run
        EnvLoader.Load();

        // Set storage connection string for tests
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")))
        {
            Environment.SetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", "UseDevelopmentStorage=true");
        }
    }

    public TestBase()
    {
        ApiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://localhost:443";
        TestApiKey = Environment.GetEnvironmentVariable("API_KEY") ?? "Cd16694YKkKxvjJC1rzCfe0T/fmQLTajDND3vQ2ic1I=";
        TestNamespace = Environment.GetEnvironmentVariable("TEST_NAMESPACE") ?? "test-namespace";

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Ignore SSL certificate errors for local development
        };

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBaseUrl)
        };
        _client.DefaultRequestHeaders.Add("X-API-Key", TestApiKey);
    }

    protected async Task<HttpContent> CreateModuleArchive(string moduleName, string version, bool validStructure = true)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);

        try
        {
            if (validStructure)
            {
                // Create valid module structure
                await File.WriteAllTextAsync(Path.Combine(tempPath, "main.tf"), "# Main configuration");
                await File.WriteAllTextAsync(Path.Combine(tempPath, "providers.tf"), "# Provider configuration");
                await File.WriteAllTextAsync(Path.Combine(tempPath, "variables.tf"), "# Variables");
                await File.WriteAllTextAsync(Path.Combine(tempPath, "outputs.tf"), "# Outputs");
            }
            else
            {
                // Create invalid module structure (missing required files)
                await File.WriteAllTextAsync(Path.Combine(tempPath, "main.tf"), "# Main configuration");
            }

            // Create .tgz archive
            var archivePath = Path.Combine(Path.GetTempPath(), $"{moduleName}-{version}.tgz");
            await CreateTgzArchive(tempPath, archivePath);

            // Create multipart form content
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(archivePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-gzip");
            content.Add(fileContent, "file", $"{moduleName}-{version}.tgz");

            return content;
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }
    }

    private async Task CreateTgzArchive(string sourcePath, string destinationPath)
    {
        // Create tar.gz archive using system tar command
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-czf {destinationPath} -C {sourcePath} .",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var process = System.Diagnostics.Process.Start(startInfo);
        await process!.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Failed to create tar.gz archive: {await process.StandardError.ReadToEndAsync()}");
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}