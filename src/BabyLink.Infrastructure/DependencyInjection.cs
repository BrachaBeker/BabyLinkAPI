// AddInfrastructure: רישום שירותי Infrastructure ב-DI (בשלב זה רק AppDbContext, Scoped).
// נקראת מ-Program.cs - זו הסיבה היחידה ש-Api מפנה ל-Infrastructure.
using BabyLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing (set it in User Secrets).");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
