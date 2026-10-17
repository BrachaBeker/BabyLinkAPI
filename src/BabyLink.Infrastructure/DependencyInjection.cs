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

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
