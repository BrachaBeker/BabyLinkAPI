// מתודת הרחבה AddInfrastructure(this IServiceCollection services, IConfiguration config).
// רושמת: AppDbContext עם UseNpgsql (Scoped), Repositories (Scoped),
// PasswordHasher, JwtTokenGenerator, וקישור JwtOptions לסעיף "Jwt".
// נקראת מ-Program.cs - זו הסיבה היחידה ש-Api מפנה ל-Infrastructure.
