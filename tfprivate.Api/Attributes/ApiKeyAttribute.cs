using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace tfprivate.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyEnvName = "API_KEY";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var potentialApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                errors = new[] { new { detail = $"Missing {ApiKeyHeaderName} header" } }
            });
            return;
        }

        // Try to get API key from environment variable first
        var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvName);

        // If not found in environment, try configuration
        if (string.IsNullOrEmpty(apiKey))
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            apiKey = configuration.GetValue<string>("ApiKey");
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        if (!apiKey.Equals(potentialApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                errors = new[] { new { detail = "Invalid API key" } }
            });
            return;
        }

        await next();
    }
}