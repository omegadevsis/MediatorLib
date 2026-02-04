using System.Reflection;
using MediatorLib.Notifications;
using MediatorLib.Pipeline;
using MediatorLib.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MediatorLib;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        // Registra handlers
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

        // Mediator deve ser Scoped para usar handlers que dependem de DbContext
        services.AddScoped<Mediator>();
        return services;
    }
}