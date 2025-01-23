using System.Net;
using Xunit;
using System.Net.Http.Json;

namespace tfprivate.Tests;

public class ModuleUploadTests : TestBase
{
    [Fact]
    public async Task UploadModule_WithValidStructure_ShouldSucceed()
    {
        // Arrange
        var moduleName = "test-module";
        var version = "1.0.0";
        var content = await CreateModuleArchive(moduleName, version, validStructure: true);

        // Act
        var response = await _client.PostAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UploadModule_WithInvalidStructure_ShouldFail()
    {
        // Arrange
        var moduleName = "test-module";
        var version = "1.0.0";
        var content = await CreateModuleArchive(moduleName, version, validStructure: false);

        // Act
        var response = await _client.PostAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid Terraform module structure", errorContent);
    }

    [Fact]
    public async Task UploadModule_WithoutApiKey_ShouldFail()
    {
        // Arrange
        var moduleName = "test-module";
        var version = "1.0.0";
        var content = await CreateModuleArchive(moduleName, version);
        var client = new HttpClient { BaseAddress = _client.BaseAddress }; // Client without API key

        // Act
        var response = await client.PostAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadModule_WithDuplicateVersion_ShouldFail()
    {
        // Arrange
        var moduleName = "test-module";
        var version = "1.0.0";
        var content = await CreateModuleArchive(moduleName, version);

        // Upload first time
        await _client.PostAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}",
            content);

        // Act - Upload same version again
        var response = await _client.PostAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("already exists", errorContent);
    }

    [Theory]
    [InlineData("invalid-version")]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    public async Task UploadModule_WithInvalidVersion_ShouldFail(string version)
    {
        // Arrange
        var moduleName = "test-module";
        var content = await CreateModuleArchive(moduleName, version);

        // Act
        var response = await _client.PostAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}