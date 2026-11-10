
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CoffeeAndChill.ApiGateway.Middleware
{
    /// <summary>
    /// Catches unhandled exceptions and returns a consistent ProblemDetails response.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Request cancelled by client: {Path}",
                    context.Request.Path);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Backend request timed out: {Path}", context.Request.Path);

                await WriteProblem(
                    context,
                    StatusCodes.Status504GatewayTimeout,
                    "Gateway Timeout",
                    "The backend service took too long to respond.",
                    ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Backend unreachable: {Path}", context.Request.Path);

                await WriteProblem(
                    context,
                    StatusCodes.Status502BadGateway,
                    "Bad Gateway",
                    "The backend service could not be reached.",
                    ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Path}", context.Request.Path);

                await WriteProblem(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred.",
                    ex);
            }
        }

        private async Task WriteProblem(
            HttpContext context,
            int status,
            string title,
            string detail,
            Exception ex)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;

            if (_env.IsDevelopment())
            {
                problem.Extensions["error"] = ex.Message;
            }

            var json = JsonSerializer.Serialize(problem);

            await context.Response.WriteAsync(json);
        }
    }
}
