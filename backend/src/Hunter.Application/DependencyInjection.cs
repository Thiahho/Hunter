using Hunter.Application.Auth;
using Hunter.Application.Campaigning;
using Hunter.Application.Compliance;
using Hunter.Application.Crm;
using Hunter.Application.Finance;
using Hunter.Application.Metrics;
using Hunter.Application.Prospecting;
using Hunter.Application.Sales;
using Microsoft.Extensions.DependencyInjection;

namespace Hunter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProspectDuplicateFinder, ProspectDuplicateFinder>();
        services.AddScoped<IProspectService, ProspectService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IScheduledProspectAutomationService, ScheduledProspectAutomationService>();
        services.AddScoped<ISuppressionService, SuppressionService>();
        services.AddScoped<IMessageTemplateService, MessageTemplateService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IMessageQueryService, MessageQueryService>();
        services.AddScoped<IMessageResponseQueryService, MessageResponseQueryService>();
        services.AddScoped<IInboundMessageService, InboundMessageService>();
        services.AddScoped<ITestMessageService, TestMessageService>();
        services.AddScoped<IScheduledMessageService, ScheduledMessageService>();
        services.AddScoped<IMessageStatusService, MessageStatusService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<ISaleQueryService, SaleQueryService>();
        services.AddScoped<ICostService, CostService>();
        services.AddScoped<IMetricsService, MetricsService>();

        return services;
    }
}
