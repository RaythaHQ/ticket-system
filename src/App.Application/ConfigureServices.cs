using System.Reflection;
using App.Application.Common.Behaviors;
using App.Application.Common.Interfaces;
using App.Application.Common.Services;
using App.Application.NotificationPreferences.Services;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace App.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediator(options =>
        {
            options.Namespace = "Raytha.Application.Mediator";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        // Ticketing system services
        services.AddScoped<ITicketPermissionService, TicketPermissionService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();

        return services;
    }
}
