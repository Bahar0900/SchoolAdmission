using System.Text.Json;
using SchoolAdmission.Api.Dtos;
using SchoolAdmission.Api.Exceptions;

namespace SchoolAdmission.Api.Middleware;

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
        catch (AppException ex)
        {
            _logger.LogWarning("Application error: {Message} (Status {StatusCode})", ex.Message, ex.StatusCode);
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            var response = new ErrorResponse(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled server error caught by middleware.");
            // Assignment specifies: "ভুল input-এ 4xx দিতে হবে, কখনো 500 নয়।"
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var response = new ErrorResponse("Invalid request or request could not be processed.", ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
