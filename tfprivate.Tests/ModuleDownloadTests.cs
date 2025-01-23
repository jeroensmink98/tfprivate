using System.Net;
using Xunit;

namespace tfprivate.Tests;

public class ModuleDownloadTests : TestBase
{
    [Fact]
    public async Task GetModule_WithValidVersion_ShouldReturnDownloadUrl()
    {
        // Arrange
        var moduleName = "test-module";
        var version = "1.0.0";
        var content = await CreateModuleArchive(moduleName, version);

        // Upload module first
        await _client.PostAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}",
            content);

        // Act
        var response = await _client.GetAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Terraform-Get"));
        var downloadUrl = response.Headers.GetValues("X-Terraform-Get").First();
        Assert.Contains("module.tgz", downloadUrl);
    }

    [Fact]
    public async Task GetModule_WithNonExistentVersion_ShouldReturn404()
    {
        // Arrange
        var moduleName = "test-module";
        var version = "1.0.0";

        // Act
        var response = await _client.GetAsync(
            $"/v1/module/{TestNamespace}/{moduleName}/{version}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestModule_WithMultipleVersions_ShouldReturnLatest()
    {
        // Arrange
        var moduleName = "test-module";
        var versions = new[] { "1.0.0", "1.1.0", "1.0.1" };

        // Upload multiple versions
        foreach (var version in versions)
        {
            var content = await CreateModuleArchive(moduleName, version);
            await _client.PostAsync(
                $"/v1/module/{TestNamespace}/{moduleName}/{version}",
                content);
        }

        // Act
        var response = await _client.GetAsync(
            $"/v1/module/{TestNamespace}/{moduleName}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Terraform-Get"));
        var downloadUrl = response.Headers.GetValues("X-Terraform-Get").First();
        Assert.Contains("1.1.0", downloadUrl); // Should return highest semantic version
    }

    [Fact]
    public async Task ListModuleVersions_ShouldReturnAllVersions()
    {
        // Arrange
        var moduleName = "test-module";
        var versions = new[] { "1.0.0", "1.1.0", "1.0.1" };

        // Upload multiple versions
        foreach (var version in versions)
        {
            var moduleContent = await CreateModuleArchive(moduleName, version);
            await _client.PostAsync(
                $"/v1/module/{TestNamespace}/{moduleName}/{version}",
                moduleContent);
        }

        // Act
        var response = await _client.GetAsync(
            $"/v1/modules/{TestNamespace}/{moduleName}/versions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        foreach (var version in versions)
        {
            Assert.Contains(version, responseContent);
        }
    }

    [Fact]
    public async Task ListModules_ShouldReturnPaginatedResults()
    {
        // Arrange
        var moduleNames = new[] { "module1", "module2", "module3" };
        var version = "1.0.0";

        // Upload multiple modules
        foreach (var moduleName in moduleNames)
        {
            var moduleContent = await CreateModuleArchive(moduleName, version);
            await _client.PostAsync(
                $"/v1/module/{TestNamespace}/{moduleName}/{version}",
                moduleContent);
        }

        // Act
        var response = await _client.GetAsync(
            $"/v1/modules/{TestNamespace}?limit=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("next_offset", responseContent);
        Assert.Contains("module1", responseContent);
        Assert.Contains("module2", responseContent);
        Assert.DoesNotContain("module3", responseContent);
    }
}