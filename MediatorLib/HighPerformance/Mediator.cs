using MediatorLib.HighPerformance.Notifications;
using MediatorLib.HighPerformance.Pipeline;
using MediatorLib.HighPerformance.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MediatorLib.HighPerformance;

public class Mediator
{
    private readonly IServiceProvider _provider;

    public Mediator(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Envia uma requisição sem nenhum uso de Reflection. 
    /// O tipo é resolvido via polimorfismo (Double Dispatch).
    /// </summary>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        return request.SendTo(this, cancellationToken);
    }

    /// <summary>
    /// Método interno chamado pela implementação da Request.
    /// </summary>
    internal Task<TResponse> HandleInternal<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var handler = _provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = _provider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in behaviors.Reverse())
        {
            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle(request, next, cancellationToken);
        }

        return handlerDelegate();
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = _provider.GetServices<INotificationHandler<TNotification>>();
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}
