// נקודת הכניסה - Composition Root. גרסה מינימלית (שלב 2) כדי ש-dotnet ef יעבוד;
// הגרסה המלאה (Swagger, JWT, CORS, NLog, Middleware) תיכתב בשלב 4.
using BabyLink.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Run();
