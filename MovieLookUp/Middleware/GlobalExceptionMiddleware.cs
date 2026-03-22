using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace MovieLookUp.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Ett oväntat fel inträffade vid anrop till {Path}", context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = (int)HttpStatusCode.InternalServerError;
            var title = "Ett oväntat serverfel inträffade.";

            if (exception is KeyNotFoundException)
            {
                statusCode = (int)HttpStatusCode.NotFound;                
                title = "Resursen hittades inte.";
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = (int)HttpStatusCode.Unauthorized;
                title = "Du saknar behörighet.";
            }
            else if (exception is ArgumentException || exception is InvalidOperationException)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Ogiltig förfrågan.";
            }

            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message,
                Instance = context.Request.Path
            };

            var jsonResponse = JsonSerializer.Serialize(problemDetails);

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}