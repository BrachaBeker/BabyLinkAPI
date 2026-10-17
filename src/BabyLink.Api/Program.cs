// נקודת הכניסה - Composition Root: רישום שירותים (DI, Swagger עם Bearer, JWT, CORS, NLog),
// בניית ה-Pipeline (הסדר חשוב!) והרצת ה-Migrations/Seed בעלייה.
using System.Text;
using System.Text.Json.Serialization;
using BabyLink.Api.Middleware;
using BabyLink.Api.Swagger;
using BabyLink.Application;
using BabyLink.Application.Interfaces.Security;
using BabyLink.Application.Options;
using BabyLink.Infrastructure;
using BabyLink.Infrastructure.Persistence;
using BabyLink.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Render מגדיר את הפורט שהשרת צריך להאזין לו במשתנה הסביבה PORT.
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// ---------- Logging ----------
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// ---------- Layers ----------
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- Controllers + ProblemDetails ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// כל ProblemDetails (גם 400 של ModelState וגם של ה-Middleware) מקבל correlationId.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.Items[CorrelationIdMiddleware.ItemKey]);

// ---------- JWT Authentication ----------
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters (set it in User Secrets).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // שמות ה-Claims נשארים כמו ב-token (sub, role, name) - בלי המרה לשמות הארוכים של Microsoft.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS ----------
const string ClientCorsPolicy = "Client";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(ClientCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

// ---------- Swagger (עם כפתור Authorize ל-Bearer) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BabyLink API", Version = "v1" });
    options.AddSecurityDefinition(AuthorizeOperationFilter.SchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "מדביקים כאן את ה-token שהתקבל מ-POST /api/tokens (בלי המילה Bearer)."
    });
    // המנעול מוצג רק על endpoints שדורשים התחברות (ולא על הרשמה/התחברות).
    options.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();

// ---------- Migrations + Seed ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DataSeeder.SeedAsync(db, passwordHasher, app.Lifetime.ApplicationStopping);
}

// ---------- Pipeline (הסדר חשוב!) ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
