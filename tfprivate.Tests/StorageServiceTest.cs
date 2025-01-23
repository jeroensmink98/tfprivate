using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using tfprivate.Api.Services;
using Xunit;

namespace tfprivate.Tests;

public class StorageServiceTest
{
    private readonly StorageService _storageService;
    private const string TestContainer = "test-container";

    public StorageServiceTest()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Azure:Storage:AccountName", ""},
                {"Azure:Storage:AccountKey", ""}
            })
            .Build();



        _storageService = new StorageService(configuration);
    }

    [Fact]
    public async Task GetUploadUrl_ShouldReturnValidUrl()
    {
        // Arrange
        var blobName = $"test-blob-{Guid.NewGuid()}";

        // Act
        var uploadUrl = await _storageService.GetUploadUrlAsync(TestContainer, blobName);

        // Assert
        Assert.NotNull(uploadUrl);
        Assert.Contains(TestContainer, uploadUrl.ToString());
        Assert.Contains(blobName, uploadUrl.ToString());
    }

    [Fact]
    public async Task GetDownloadUrl_NonExistentBlob_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var blobName = $"non-existent-blob-{Guid.NewGuid()}";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _storageService.GetDownloadUrlAsync(TestContainer, blobName));
    }

}
