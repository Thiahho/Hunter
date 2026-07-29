using System.Net.Http.Headers;
using Hunter.Application.Auth;
using Hunter.Application.Campaigning;
using Hunter.Application.Common;
using Hunter.Application.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Infrastructure.Persistence;
using Hunter.Infrastructure.Prospecting;
using Hunter.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HunterDb")
            ?? throw new InvalidOperationException("Missing connection string 'HunterDb'.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddDbContext<HunterDbContext>((sp, options) =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IHunterDbContext>(sp => sp.GetRequiredService<HunterDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIntentClassifier, KeywordIntentClassifier>();

        services.Configure<WhatsAppCloudApiOptions>(configuration.GetSection(WhatsAppCloudApiOptions.SectionName));
        var whatsAppOptions = configuration.GetSection(WhatsAppCloudApiOptions.SectionName).Get<WhatsAppCloudApiOptions>();

        if (whatsAppOptions?.IsConfigured == true)
        {
            services.AddHttpClient<IMessageProvider, WhatsAppCloudApiMessageProvider>((sp, client) =>
            {
                client.BaseAddress = new Uri("https://graph.facebook.com/");
                var accessToken = sp.GetRequiredService<IOptions<WhatsAppCloudApiOptions>>().Value.AccessToken;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            });
        }
        else
        {
            services.AddScoped<IMessageProvider, StubMessageProvider>();
        }

        services.Configure<GooglePlacesOptions>(configuration.GetSection(GooglePlacesOptions.SectionName));
        services.AddHttpClient<IGooglePlacesClient, GooglePlacesClient>(client =>
        {
            client.BaseAddress = new Uri("https://places.googleapis.com/");
        });

        return services;
    }
}
