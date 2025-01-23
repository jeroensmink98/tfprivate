using Microsoft.AspNetCore.Mvc;
using tfprivate.Api.Services;
using tfprivate.Api.Attributes;
using System.ComponentModel.DataAnnotations;
using NuGet.Versioning;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace tfprivate.Api.Controllers;

/// <summary>
/// API endpoints for the private Terraform module registry
/// </summary>
[ApiController]
[Produces("application/json")]
public class ModuleRegistryController : ControllerBase
{
    private readonly IStorageService _storageService;
    private const string ModuleContainer = "modules";

    public ModuleRegistryController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    private IActionResult Error(int statusCode, string message)
    {
        return StatusCode(statusCode, new { errors = new[] { message } });
    }

    /// <summary>
    /// Lists all available API endpoints
    /// </summary>
    [HttpGet]
    [Route("/")]
    [Route("/v1")]
    public IActionResult Index()
    {
        var endpoints = new[]
        {
            new { method = "GET", path = "/v1/modules/{namespace}", description = "List all modules in a namespace" },
            new { method = "GET", path = "/v1/module/{namespace}/{module_name}", description = "Get latest version of a module" },
            new { method = "GET", path = "/v1/module/{namespace}/{module_name}/{version}", description = "Get specific version of a module" },
            new { method = "POST", path = "/v1/module/{namespace}/{module_name}/{version}", description = "Upload a new module version" },
            new { method = "GET", path = "/v1/modules/{namespace}/{module_name}/versions", description = "List all versions of a module" }
        };

        var response = new
        {
            name = "Terraform Private Registry API",
            version = "v1",
            endpoints = endpoints
        };

        // Only add documentation link in non-production environments
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
        {
            return Ok(new
            {
                name = response.name,
                version = response.version,
                endpoints = response.endpoints,
                documentation = "/swagger"
            });
        }

        return Ok(response);
    }

    [HttpGet]
    [Route("v1/modules/{namespace}")]
    public async Task<IActionResult> ListModules(
        [FromRoute] string @namespace,
        [FromQuery] int limit = 15,
        [FromQuery] int offset = 0)
    {
        try
        {
            var modules = await _storageService.ListModulesAsync(ModuleContainer, @namespace);

            // Apply pagination
            var totalCount = modules.Count();
            var hasMore = offset + limit < totalCount;
            modules = modules.Skip(offset).Take(limit).ToList();

            return Ok(new
            {
                meta = new
                {
                    limit = limit,
                    current_offset = offset,
                    next_offset = hasMore ? offset + limit : (int?)null,
                    next_url = hasMore ? $"/v1/modules/{@namespace}?limit={limit}&offset={offset + limit}" : null,
                    prev_offset = offset > 0 ? Math.Max(0, offset - limit) : (int?)null
                },
                modules = modules.Select(m => new
                {
                    id = $"{@namespace}/{m.Name}/{m.Version}",
                    owner = "",  // We don't track this currently
                    @namespace = @namespace,
                    name = m.Name,
                    version = m.Version,
                    description = m.Description ?? "",
                    source = m.Source ?? "",
                    published_at = m.PublishedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
                    verified = false  // We don't support verified modules currently
                }).ToArray()
            });
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return Error(503, "Service is currently under load, please retry later");
            }
            return Error(500, ex.Message);
        }
    }

    /// <summary>
    /// Get a specific version or latest version of a module
    /// </summary>
    /// <param name="namespace">The namespace the module is owned by</param>
    /// <param name="module_name">The name of the module</param>
    /// <returns>Module download URL</returns>
    [HttpGet]
    [Route("v1/module/{namespace}/{module_name}")]
    public async Task<IActionResult> GetLatestModule(
        [FromRoute] string @namespace,
        [FromRoute] string module_name)
    {
        try
        {
            var prefix = $"{@namespace}/{module_name}";
            var versions = await _storageService.ListVersionsAsync(ModuleContainer, prefix);

            if (!versions.Any())
            {
                return Error(404, $"Module {@namespace}/{module_name} not found");
            }

            // Parse versions and find the latest one using semantic versioning
            var latestVersion = versions
                .Select(v => SemanticVersion.Parse(v))
                .Max()
                .ToString();

            // Get the download URL for the latest version
            var blobPath = $"{@namespace}/{module_name}/v{latestVersion}/module.tgz";
            var url = await _storageService.GetDownloadUrlAsync(ModuleContainer, blobPath);

            // Return URL in X-Terraform-Get header with 204 No Content
            Response.Headers.Append("X-Terraform-Get", url.ToString());
            return NoContent();
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return Error(503, "Service is currently under load, please retry later");
            }
            return Error(500, ex.Message);
        }
    }

    [HttpGet]
    [Route("v1/module/{namespace}/{module_name}/{version}")]
    public async Task<IActionResult> GetModule(
        [FromRoute] string @namespace,
        [FromRoute][Required] string module_name,
        [FromRoute][Required][RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be in semantic versioning format (e.g. 1.0.0)")] string version)
    {
        try
        {
            var blobPath = $"{@namespace}/{module_name}/v{version}/module.tgz";
            var url = await _storageService.GetDownloadUrlAsync(ModuleContainer, blobPath);

            // Return URL in X-Terraform-Get header with 204 No Content
            Response.Headers.Append("X-Terraform-Get", url.ToString());
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return Error(404, $"Module {@namespace}/{module_name} version {version} not found");
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return Error(503, "Service is currently under load, please retry later");
            }
            return Error(500, ex.Message);
        }
    }

    /// <summary>
    /// Upload a new Terraform module version
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="module_name">Module name</param>
    /// <param name="version">Module version (semver)</param>
    /// <param name="file">The module .tgz file</param>
    /// <returns>Upload result with URL or error details</returns>
    /// <response code="200">Module uploaded successfully</response>
    /// <response code="400">Invalid request or file format</response>
    /// <response code="401">Missing or invalid API key</response>
    /// <response code="409">Module version already exists</response>
    /// <response code="500">Server error</response>
    [HttpPost]
    [ApiKey]
    [Consumes("multipart/form-data")]
    [Route("v1/module/{namespace}/{module_name}/{version}")]
    [RequestSizeLimit(52428800)] // 50MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> UploadModule(
        [FromRoute] string @namespace,
        [FromRoute][Required] string module_name,
        [FromRoute][Required][RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be in semantic versioning format (e.g. 1.0.0)")] string version,
        [FromForm] IFormFile file)
    {
        try
        {
            if (file == null)
            {
                return Error(400, "No file provided");
            }

            var blobPath = $"{@namespace}/{module_name}/v{version}/module.tgz";

            // Check if module already exists
            try
            {
                await _storageService.GetDownloadUrlAsync(ModuleContainer, blobPath);
                return Error(409, $"Module {@namespace}/{module_name} version {version} already exists");
            }
            catch (FileNotFoundException)
            {
                // This is expected - continue with upload
            }

            if (file != null && file.Length > 0)
            {
                // Handle form file upload
                if (!file.FileName.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
                {
                    return Error(400, "File must be a .tgz archive");
                }

                // Get upload URL
                var uploadUrl = await _storageService.GetUploadUrlAsync(ModuleContainer, blobPath);

                // Create metadata
                var metadata = new Dictionary<string, string>
                {
                    { "namespace", @namespace },
                    { "moduleName", module_name },
                    { "version", version }
                };

                // Upload the file directly to blob storage with metadata
                using var stream = file.OpenReadStream();
                await _storageService.UploadFromStreamAsync(ModuleContainer, blobPath, stream, metadata);

                return Ok(new { url = uploadUrl.ToString() });
            }
            else if (Request.ContentLength > 0)
            {
                // Create metadata
                var metadata = new Dictionary<string, string>
                {
                    { "namespace", @namespace },
                    { "moduleName", module_name },
                    { "version", version }
                };

                // Handle raw body upload
                using var stream = Request.Body;
                await _storageService.UploadFromStreamAsync(ModuleContainer, blobPath, stream, metadata);

                return Ok(new { message = $"Module {@namespace}/{module_name} version {version} uploaded successfully" });
            }

            return Error(400, "No file provided");
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return Error(503, "Service is currently under load, please retry later");
            }
            return Error(500, ex.Message);
        }
    }

    /// <summary>
    /// Lists available versions for a specific module
    /// </summary>
    /// <param name="namespace">The namespace the module is owned by</param>
    /// <param name="module_name">The name of the module</param>
    /// <returns>List of available versions for the module</returns>
    /// <response code="200">List of available versions</response>
    /// <response code="404">Module not found</response>
    /// <response code="500">Server error</response>
    [HttpGet]
    [Route("v1/modules/{namespace}/{module_name}/versions")]
    public async Task<IActionResult> ListModuleVersions(
        [FromRoute] string @namespace,
        [FromRoute] string module_name)
    {
        try
        {
            var prefix = $"{@namespace}/{module_name}";
            var versions = await _storageService.ListVersionsAsync(ModuleContainer, prefix);

            if (!versions.Any())
            {
                return Error(404, $"Module {@namespace}/{module_name} not found");
            }

            return Ok(new
            {
                modules = new[]
                {
                    new
                    {
                        versions = versions.Select(v => new { version = v }).ToArray()
                    }
                }
            });
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return Error(503, "Service is currently under load, please retry later");
            }
            return Error(500, ex.Message);
        }
    }
}

