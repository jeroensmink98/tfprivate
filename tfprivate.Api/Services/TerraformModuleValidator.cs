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

            // Get all files in the root directory and immediate subdirectories
            var allFiles = Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .ToList();

            // Group files by directory
            var filesByDirectory = allFiles
                .GroupBy(f => f.DirectoryName ?? tempPath)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Name).ToList());

            // Find the directory that contains the most required files
            var validDirectories = filesByDirectory
                .Where(kvp => HasRequiredFiles(kvp.Value))
                .ToList();

            if (!validDirectories.Any())
            {
                result.Errors.Add("No valid Terraform module structure found in the archive");
                result.IsValid = false;
                return result;
            }

            // If we have multiple valid directories, prefer the root directory if it's valid
            var moduleFiles = validDirectories
                .FirstOrDefault(d => d.Key.Equals(tempPath, StringComparison.OrdinalIgnoreCase))
                .Value ?? validDirectories.First().Value;

            result.FoundFiles = moduleFiles;

            // Check required files
            foreach (var requiredFile in _requiredFiles)
            {
                if (!moduleFiles.Any(f => f.Equals(requiredFile, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Errors.Add($"Required file '{requiredFile}' is missing");
                }
            }

            // Check if at least one variables file exists
            if (!moduleFiles.Any(f => f.Equals("variables.tf", StringComparison.OrdinalIgnoreCase) ||
                                  f.Equals("variable.tf", StringComparison.OrdinalIgnoreCase)))
            {
                result.Errors.Add("No variables file found. Either 'variables.tf' or 'variable.tf' is required");
            }

            // Check if at least one outputs file exists
            if (!moduleFiles.Any(f => f.Equals("outputs.tf", StringComparison.OrdinalIgnoreCase) ||
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

    private bool HasRequiredFiles(IEnumerable<string> files)
    {
        // Check if the directory has at least one required file and one of each required type
        return files.Any(f => _requiredFiles.Contains(f, StringComparer.OrdinalIgnoreCase)) &&
               files.Any(f => f.Equals("variables.tf", StringComparison.OrdinalIgnoreCase) ||
                             f.Equals("variable.tf", StringComparison.OrdinalIgnoreCase)) &&
               files.Any(f => f.Equals("outputs.tf", StringComparison.OrdinalIgnoreCase) ||
                             f.Equals("output.tf", StringComparison.OrdinalIgnoreCase));
    }

    private async Task ExtractTgzArchive(Stream archiveStream, string destinationPath)
    {
        // Create a memory stream to buffer the input
        using var memoryStream = new MemoryStream();
        await archiveStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var gzipStream = new GZipInputStream(memoryStream);
        using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, System.Text.Encoding.UTF8);

        tarArchive.ExtractContents(destinationPath);
    }
}