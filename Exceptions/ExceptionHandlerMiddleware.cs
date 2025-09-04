using InventoryManagement.Web.Exceptions;
using InventoryManagement.Web.Models;
using Serilog;
using System.Net;
using System.Text.Json;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var statusCode = HttpStatusCode.InternalServerError;
        var errorResponse = new ErrorResponse { Message = "An unexpected error occurred." };
        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse = new ErrorResponse
                {
                    Message = "Validation failed.",
                    Errors = validationException.Errors
                };
                break;
            default:
                Log.Error(exception, "An unhandled exception occurred.");
                break;
        }
        context.Response.StatusCode = (int)statusCode;
        var result = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(result);
    }
}