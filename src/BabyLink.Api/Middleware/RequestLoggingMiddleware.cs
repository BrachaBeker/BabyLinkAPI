// לוג Information לכל בקשה: Method, Path, StatusCode ומשך זמן.
// לא נכתבים Body, Headers או Query - כדי לא לחשוף סיסמאות ו-tokens.
using System.Diagnostics;

namespace BabyLink.Api.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            // החריגה עצמה נרשמת ומטופלת ב-ExceptionHandlingMiddleware (שעוטף אותנו).
            logger.LogInformation("HTTP {Method} {Path} failed with an exception after {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
