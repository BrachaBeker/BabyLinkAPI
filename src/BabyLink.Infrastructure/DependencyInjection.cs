// AddInfrastructure: רישום AppDbContext (Scoped), Repositories (Scoped), Security (Singleton - בלי מצב),
// וקישור JwtOptions לסעיף "Jwt". נקראת מ-Program.cs - זו הסיבה היחידה ש-Api מפנה ל-Infrastructure.
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Security;
using BabyLink.Application.Options;
using BabyLink.Infrastructure.Persistence;
using BabyLink.Infrastructure.Repositories;
using BabyLink.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BabyLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' is missing (set it in User Secrets).");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(NormalizeConnectionString(connectionString)));

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

    // Render נותן Connection String בפורמט URL (postgresql://user:pass@host:port/db),
    // ו-Npgsql צריך פורמט Key=Value. מחרוזת שכבר בפורמט של Npgsql חוזרת כמו שהיא.
    private static string NormalizeConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Prefer
        }.ConnectionString;
    }
}
