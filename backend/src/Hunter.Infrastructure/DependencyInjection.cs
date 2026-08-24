using System.Net.Http.Headers;
using Hunter.Application.Auth;
using Hunter.Application.Campaigning;
using Hunter.Application.Common;
using Hunter.Application.Crm;
using Hunter.Application.Prospecting;
using Hunter.Infrastructure.BackgroundJobs;
using Hunter.Infrastructure.GoogleDrive;
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
        services.AddScoped<IAutoReplyDetector, AutoReplyDetector>();
        services.Configure<AutoReplyFollowUpOptions>(configuration.GetSection(AutoReplyFollowUpOptions.SectionName));

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

            services.AddHttpClient<IWhatsAppTemplateCatalogClient, WhatsAppTemplateCatalogClient>((sp, client) =>
            {
                client.BaseAddress = new Uri("https://graph.facebook.com/");
                var accessToken = sp.GetRequiredService<IOptions<WhatsAppCloudApiOptions>>().Value.AccessToken;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            });
        }
        else
        {
            services.AddScoped<IMessageProvider, StubMessageProvider>();
            services.AddScoped<IWhatsAppTemplateCatalogClient, StubWhatsAppTemplateCatalogClient>();
        }

        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        var telegramOptions = configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>();

        if (telegramOptions?.IsConfigured == true)
        {
            services.AddHttpClient<ITelegramNotifier, TelegramNotifier>(client =>
            {
                client.BaseAddress = new Uri("https://api.telegram.org/");
            });
        }
        else
        {
            services.AddScoped<ITelegramNotifier, StubTelegramNotifier>();
        }

        services.Configure<GoogleDriveOptions>(configuration.GetSection(GoogleDriveOptions.SectionName));
        var googleDriveOptions = configuration.GetSection(GoogleDriveOptions.SectionName).Get<GoogleDriveOptions>();

        if (googleDriveOptions?.IsConfigured == true)
            services.AddScoped<IGoogleDriveClient, GoogleDriveClient>();
        else
            services.AddScoped<IGoogleDriveClient, StubGoogleDriveClient>();

        services.Configure<ProspectDriveSyncOptions>(configuration.GetSection(ProspectDriveSyncOptions.SectionName));
        services.AddHostedService<ProspectDriveSyncBackgroundService>();

        services.Configure<CampaignQueueOptions>(configuration.GetSection(CampaignQueueOptions.SectionName));
        services.AddHostedService<CampaignQueueBackgroundService>();

        services.Configure<ScheduledProspectAutomationOptions>(configuration.GetSection(ScheduledProspectAutomationOptions.SectionName));
        services.AddHostedService<ScheduledProspectAutomationBackgroundService>();

        services.Configure<ScheduledMessageOptions>(configuration.GetSection(ScheduledMessageOptions.SectionName));
        services.AddHostedService<ScheduledMessageBackgroundService>();

        services.AddScoped<IStaleLeadEscalationService, StaleLeadEscalationService>();
        services.Configure<StaleLeadEscalationOptions>(configuration.GetSection(StaleLeadEscalationOptions.SectionName));
        services.AddHostedService<StaleLeadEscalationBackgroundService>();

        services.Configure<GooglePlacesOptions>(configuration.GetSection(GooglePlacesOptions.SectionName));
        services.AddHttpClient<IGooglePlacesClient, GooglePlacesClient>(client =>
        {
            client.BaseAddress = new Uri("https://places.googleapis.com/");
        });

        services.Configure<ApifyOptions>(configuration.GetSection(ApifyOptions.SectionName));
        services.AddHttpClient<IApifyGoogleMapsClient, ApifyGoogleMapsClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ApifyOptions>>().Value;
            client.BaseAddress = new Uri("https://api.apify.com/");
            // Margen sobre el timeout que ya le pedimos a Apify en la query string
            // (run-sync-get-dataset-items?timeout=N): mismo criterio que OpenStreetMapClient, para
            // no cortar la conexión antes de que el servidor responda.
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds + 20);
        });

        services.Configure<NominatimOptions>(configuration.GetSection(NominatimOptions.SectionName));
        services.AddHttpClient<INominatimClient, NominatimClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<NominatimOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        services.Configure<OpenStreetMapOptions>(configuration.GetSection(OpenStreetMapOptions.SectionName));
        services.AddHttpClient<IOpenStreetMapClient, OpenStreetMapClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenStreetMapOptions>>().Value;
            // Overpass no cobra por request, pero sí es lento bajo carga pública: se le da
            // margen sobre el [timeout:N] que la propia query le pide al servidor, para no
            // cortar la conexión antes de que Overpass responda con un 504 legible.
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds + 30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
        });

        return services;
    }
}
