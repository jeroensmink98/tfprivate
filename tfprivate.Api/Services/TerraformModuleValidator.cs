using System.IO.Compression;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace tfprivate.Api.Services;

public class TerraformModuleValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> FoundFiles { get; set; } = new();
}

public interface ITerraformModuleValidator
{
    Task<TerraformModuleValidationResult> ValidateModuleArchiveAsync(Stream archiveStream);
}

public class TerraformModuleValidator : ITerraformModuleValidator
{
    private readonly string[] _requiredFiles = new[] { "main.tf", "providers.tf" };
    private readonly string[] _optionalFiles = new[] { "variables.tf", "variable.tf", "outputs.tf", "output.tf", "README.md", "readme.md" };
    private readonly ILogger<TerraformModuleValidator> _logger;

    public TerraformModuleValidator(ILogger<TerraformModuleValidator> logger)
    {
        _logger = logger;
    }

    public async Task<TerraformModuleValidationResult> ValidateModuleArchiveAsync(Stream archiveStream)
    {
        var result = new TerraformModuleValidationResult();
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(tempPath);
            await ExtractTgzArchive(archiveStream, tempPath);

            // Get all files in the root directory
            var rootFiles = Directory.GetFiles(tempPath)
                .Select(f => Path.GetFileName(f))
                .ToList();

            result.FoundFiles = rootFiles;

            // Check required files
            foreach (var requiredFile in _requiredFiles)
            {
                if (!rootFiles.Any(f => f.Equals(requiredFile, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Errors.Add($"Required file '{requiredFile}' is missing from the root directory");
                }
            }

            // Check if at least one variables file exists
            if (!rootFiles.Any(f => f.Equals("variables.tf", StringComparison.OrdinalIgnoreCase) ||
                                  f.Equals("variable.tf", StringComparison.OrdinalIgnoreCase)))
            {
                result.Errors.Add("No variables file found. Either 'variables.tf' or 'variable.tf' is required");
            }

            // Check if at least one outputs file exists
            if (!rootFiles.Any(f => f.Equals("outputs.tf", StringComparison.OrdinalIgnoreCase) ||
                                  f.Equals("output.tf", StringComparison.OrdinalIgnoreCase)))
            {
                result.Errors.Add("No outputs file found. Either 'outputs.tf' or 'output.tf' is required");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Terraform module archive");
            result.IsValid = false;
            result.Errors.Add($"Failed to validate module archive: {ex.Message}");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary directory {TempPath}", tempPath);
            }
        }

        return result;
    }

    private async Task ExtractTgzArchive(Stream archiveStream, string destinationPath)
    {
        using var gzipStream = new GZipInputStream(archiveStream);
        using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, System.Text.Encoding.UTF8);

        tarArchive.ExtractContents(destinationPath);
        await Task.CompletedTask; // Since TarArchive doesn't have async methods
    }
}