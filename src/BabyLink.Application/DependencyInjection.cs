// AddApplication: רישום AutoMapper (Profiles מה-Assembly, License Key מ-User Secrets) ו-Services (Scoped).
// בלי License Key ספריית AutoMapper עובדת, רק כותבת אזהרה ללוג. (ה-Services יירשמו בשלבים 5-7.)
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabyLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAutoMapper(cfg => cfg.LicenseKey = config["AutoMapper:LicenseKey"], typeof(DependencyInjection).Assembly);

        return services;
    }
}
