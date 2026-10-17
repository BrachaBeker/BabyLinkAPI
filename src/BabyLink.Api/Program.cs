// נקודת הכניסה - Composition Root.
// רישום: AddApplication(), AddInfrastructure(config), Controllers, Swagger (עם Bearer),
//        JWT Authentication + Authorization, CORS ללקוח, NLog (builder.Host.UseNLog()).
// Pipeline (הסדר חשוב!):
//   ExceptionHandlingMiddleware -> CorrelationIdMiddleware -> RequestLoggingMiddleware
//   -> Swagger -> CORS -> Authentication -> Authorization -> MapControllers.
// בעלייה: הרצת DataSeeder (migrations + seed).
