// קורא X-Correlation-ID מה-Header (או יוצר חדש), שומר ב-HttpContext.Items, מחזיר ב-Response Header,
// ופותח Logging Scope כך שה-CorrelationId מופיע בכל שורת לוג של הבקשה.
namespace BabyLink.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        // ערך מהלקוח מתקבל רק אם הוא קצר וסביר - כדי שלא יזריקו טקסט ארוך ללוג.
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            await next(context);
        }
    }
}
