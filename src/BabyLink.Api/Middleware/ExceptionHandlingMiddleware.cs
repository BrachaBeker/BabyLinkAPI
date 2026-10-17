// Middleware ראשון ב-Pipeline: ממפה חריגות לתשובת ProblemDetails אחידה (כולל correlationId).
// 409 נרשם כ-Warning, 500 כ-Error ובלי לחשוף פרטים ללקוח.
using BabyLink.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BabyLink.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IProblemDetailsService problemDetailsService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // הלקוח ניתק באמצע - זו לא תקלה בשרת.
            logger.LogInformation("Request {Path} was cancelled by the client", context.Request.Path);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey];
            var (status, title, detail) = Map(ex);

            if (status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            }
            else if (status == StatusCodes.Status409Conflict)
            {
                logger.LogWarning("Conflict ({ExceptionType}): {Message}. CorrelationId: {CorrelationId}",
                    ex.GetType().Name, ex.Message, correlationId);
            }
            else
            {
                logger.LogInformation("Request rejected with {StatusCode}: {Message}. CorrelationId: {CorrelationId}",
                    status, ex.Message, correlationId);
            }

            context.Response.StatusCode = status;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = ex,
                ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
            });
        }
    }

    private static (int Status, string Title, string? Detail) Map(Exception ex) => ex switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
        ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
        BusinessRuleException => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
        InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
        ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "אירעה שגיאה בשרת. יש לנסות שוב מאוחר יותר.")
    };
}
