using MediatorLib.Notifications;
using MediatorLib.Pipeline;
using MediatorLib.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MediatorLib;

public class Mediator
{
    private readonly IServiceProvider _provider;

    public Mediator(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Resolve handler fortemente tipado
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _provider.GetRequiredService(handlerType);

        // Constrói a chamada inicial
        Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> invoke = async (r, ct) =>
        {
            // Chamada via reflection
            var method = handlerType.GetMethod("Handle")!;
            var task = (Task<TResponse>)method.Invoke(handler, new object[] { r, ct })!;
            return await task;
        };

        // Resolve pipelines
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var behaviors = _provider.GetServices(behaviorType).Cast<object>().Reverse().ToList();

        foreach (var behavior in behaviors)
        {
            var next = invoke;
            invoke = (r, ct) =>
            {
                var method = behaviorType.GetMethod("Handle")!;
                var task = (Task<TResponse>)method.Invoke(behavior, new object[] { r, next, ct })!;
                return task;
            };
        }

        return await invoke(request, cancellationToken);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = _provider.GetServices<INotificationHandler<TNotification>>();
        foreach (var handler in handlers)
            await handler.Handle(notification, cancellationToken);
    }
}
