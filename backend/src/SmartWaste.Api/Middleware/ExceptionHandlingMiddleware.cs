using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartWaste.Application.Common.Exceptions;

namespace SmartWaste.Api.Middleware;

/// <summary>
/// Centralized exception handling middleware that catches application and unexpected exceptions,
/// mapping them to consistent ProblemDetails responses without leaking sensitive internals or stack traces.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred while processing request {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            DuplicateEmailException dupEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Conflict,
                Title = "Email Conflict",
                Detail = dupEx.Message,
                Instance = context.Request.Path
            },
            InvalidCredentialsException credEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Authentication Failed",
                Detail = credEx.Message,
                Instance = context.Request.Path
            },
            AccountInactiveException inactEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Account Inactive",
                Detail = inactEx.Message,
                Instance = context.Request.Path
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Resource Not Found",
                Detail = notFoundEx.Message,
                Instance = context.Request.Path
            },
            IdentityOperationException idEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Identity Error",
                Detail = string.Join("; ", idEx.Errors),
                Instance = context.Request.Path
            },
            AiServiceUnavailableException aiEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.ServiceUnavailable,
                Title = "AI Service Unavailable",
                Detail = aiEx.Message,
                Instance = context.Request.Path
            },
            UnsupportedClientRoleException unsupEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Unsupported Client Role",
                Detail = unsupEx.Message,
                Instance = context.Request.Path,
                Extensions = { ["errorCode"] = unsupEx.ErrorCode }
            },
            PasswordChangeRequiredException pwdReqEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Password Change Required",
                Detail = pwdReqEx.Message,
                Instance = context.Request.Path
            },
            InvalidRoleException invRoleEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Invalid Role",
                Detail = invRoleEx.Message,
                Instance = context.Request.Path
            },
            UserManagementException userMgmtEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "User Management Error",
                Detail = userMgmtEx.Message,
                Instance = context.Request.Path
            },
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing your request. Please try again later.",
                Instance = context.Request.Path
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
