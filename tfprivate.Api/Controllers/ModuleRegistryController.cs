using Microsoft.AspNetCore.Mvc;
using tfprivate.Api.Services;
using tfprivate.Api.Attributes;
using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Models;

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

    [HttpGet]
    [Route("api/v1/modules/{org}")]
    public async Task<IActionResult> ListModules([FromRoute] string org)
    {
        try
        {
            var modules = await _storageService.ListModulesAsync(ModuleContainer, org);

            return Ok(new
            {
                modules = modules.Select(m => new
                {
                    id = Guid.NewGuid(),
                    module = m,
                    org = org
                }).ToArray()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { new { detail = ex.Message } } });
        }
    }

    [HttpGet]
    [Route("api/v1/module/{org}/{module}/{version}")]
    public async Task<IActionResult> GetModule(
        [FromRoute] string org,
        [FromRoute][Required] string @module,
        [FromRoute][Required][RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be in semantic versioning format (e.g. 1.0.0)")] string version)
    {
        try
        {
            var blobPath = $"{org}/{module}/v{version}/module.tgz";
            var url = await _storageService.GetDownloadUrlAsync(ModuleContainer, blobPath);

            // Return direct URL to the .tgz archive
            return Content(url.ToString(), "text/plain");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { errors = new[] { new { detail = $"Module {org}/{module} version {version} not found" } } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { new { detail = ex.Message } } });
        }
    }

    /// <summary>
    /// Upload a new Terraform module version
    /// </summary>
    /// <param name="org">Organization name</param>
    /// <param name="module">Module name</param>
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
    [Route("api/v1/module/{org}/{module}/{version}")]
    [RequestSizeLimit(52428800)] // 50MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<IActionResult> UploadModule(
        [FromRoute] string org,
        [FromRoute][Required] string @module,
        [FromRoute][Required][RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be in semantic versioning format (e.g. 1.0.0)")] string version,
        [FromForm] IFormFile file)
    {
        try
        {
            if (file == null)
            {
                return BadRequest(new { errors = new[] { new { detail = "No file provided" } } });
            }

            var blobPath = $"{org}/{module}/v{version}/module.tgz";

            // Check if module already exists
            try
            {
                await _storageService.GetDownloadUrlAsync(ModuleContainer, blobPath);
                return Conflict(new { errors = new[] { new { detail = $"Module {org}/{module} version {version} already exists" } } });
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
                    return BadRequest(new { errors = new[] { new { detail = "File must be a .tgz archive" } } });
                }

                // Get upload URL
                var uploadUrl = await _storageService.GetUploadUrlAsync(ModuleContainer, blobPath);

                // Upload the file directly to blob storage
                using var stream = file.OpenReadStream();
                await _storageService.UploadFromStreamAsync(ModuleContainer, blobPath, stream);

                return Ok(new { url = uploadUrl.ToString() });
            }
            else if (Request.ContentLength > 0)
            {
                // Handle raw body upload
                using var stream = Request.Body;
                await _storageService.UploadFromStreamAsync(ModuleContainer, blobPath, stream);

                return Ok(new { message = $"Module {org}/{module} version {version} uploaded successfully" });
            }

            return BadRequest(new { errors = new[] { new { detail = "No file provided" } } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errors = new[] { new { detail = ex.Message } } });
        }
    }
}

