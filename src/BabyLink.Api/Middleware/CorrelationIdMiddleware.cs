// Middleware שקורא X-Correlation-ID מה-Header (או יוצר Guid חדש),
// שומר ב-HttpContext.Items, מחזיר ב-Response Header,
// ופותח ILogger.BeginScope כך שה-CorrelationId מופיע בכל שורת לוג של הבקשה.
