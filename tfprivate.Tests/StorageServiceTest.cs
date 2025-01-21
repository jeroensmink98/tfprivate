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
                {"Azure:Storage:AccountName", "satfmodulestolx00"},
                {"Azure:Storage:AccountKey", "13qxndyQZ6qnOLnX3ZFDVUD11GoVZk0vrD5E1nRX0HS60GwfssDcAJW3NMr1bS5feJ2LITtftycb"}
            })
            .Build();

        /**
        "AccountName": "satfmodulestolx00",
        "AccountKey": "13qxndyQZ6qnOLnX3ZFDVUD11GoVZk0vrD5E1nRX0HS60GwfssDcAJW3NMr1bS5feJ2LITtftycb"
        **/

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
