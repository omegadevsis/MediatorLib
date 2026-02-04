using System.Reflection;
using MediatorLib.HighPerformance.Notifications;
using MediatorLib.HighPerformance.Pipeline;
using MediatorLib.HighPerformance.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MediatorLib.HighPerformance;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatorHighPerformance(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                              (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                               i.GetGenericTypeDefinition() == typeof(INotificationHandler<>) ||
                               i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))));

            foreach (var type in handlerTypes)
            {
                foreach (var iface in type.GetInterfaces()
                             .Where(i => i.IsGenericType &&
                                         (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                                          i.GetGenericTypeDefinition() == typeof(INotificationHandler<>) ||
                                          i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))))
                {
                    services.AddTransient(iface, type);
                }
            }
        }

        services.AddScoped<Mediator>();
        return services;
    }
}
