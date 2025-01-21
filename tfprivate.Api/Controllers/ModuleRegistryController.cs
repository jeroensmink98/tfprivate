using Microsoft.AspNetCore.Mvc;
using tfprivate.Api.Services;
using System.ComponentModel.DataAnnotations;

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
}

