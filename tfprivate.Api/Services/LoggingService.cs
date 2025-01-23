using Microsoft.Extensions.Logging;

namespace tfprivate.Api.Services;

public interface ILoggingService
{
    void LogApiRoute(string method, string path, string? username = null);
    void LogModuleValidation(string @namespace, string moduleName, string version, bool isValid, IEnumerable<string> errors);
    void LogModuleUpload(string @namespace, string moduleName, string version, bool success, string? error = null);
    void LogModuleDownload(string @namespace, string moduleName, string version);
}

public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    public void LogApiRoute(string method, string path, string? username = null)
    {
        var userInfo = !string.IsNullOrEmpty(username) ? $" by user {username}" : "";
        _logger.LogInformation("API Route accessed: {Method} {Path}{UserInfo}", method, path, userInfo);
    }

    public void LogModuleValidation(string @namespace, string moduleName, string version, bool isValid, IEnumerable<string> errors)
    {
        if (isValid)
        {
            _logger.LogInformation(
                "Module validation successful: {@namespace}/{moduleName} v{version}",
                @namespace, moduleName, version);
        }
        else
        {
            _logger.LogWarning(
                "Module validation failed: {@namespace}/{moduleName} v{version}. Errors: {Errors}",
                @namespace, moduleName, version, string.Join(", ", errors));
        }
    }

    public void LogModuleUpload(string @namespace, string moduleName, string version, bool success, string? error = null)
    {
        if (success)
        {
            _logger.LogInformation(
                "Module uploaded successfully: {@namespace}/{moduleName} v{version}",
                @namespace, moduleName, version);
        }
        else
        {
            _logger.LogError(
                "Module upload failed: {@namespace}/{moduleName} v{version}. Error: {Error}",
                @namespace, moduleName, version, error);
        }
    }

    public void LogModuleDownload(string @namespace, string moduleName, string version)
    {
        _logger.LogInformation(
            "Module downloaded: {@namespace}/{moduleName} v{version}",
            @namespace, moduleName, version);
    }
}