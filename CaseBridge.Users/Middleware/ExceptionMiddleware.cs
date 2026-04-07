using System.Net;
using System.Text.Json;

namespace CaseBridge_Users.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Move to the next part of the app
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message); // Log the error to the console
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error. CaseBridge developers are on it!",
                Detail = exception.Message // In production, hide 'Detail' for security
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}