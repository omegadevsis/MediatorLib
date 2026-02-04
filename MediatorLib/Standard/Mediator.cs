using System.Collections.Concurrent;
using MediatorLib.Standard.Notifications;
using MediatorLib.Standard.Pipeline;
using MediatorLib.Standard.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace MediatorLib.Standard;

public class Mediator
{
    private readonly IServiceProvider _provider;
    private static readonly ConcurrentDictionary<Type, object> _wrapperCache = new();

    public Mediator(IServiceProvider provider)
    {
        _provider = provider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        
        var wrapper = (RequestHandlerWrapperBase<TResponse>)_wrapperCache.GetOrAdd(requestType, t =>
        {
            var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(t, typeof(TResponse));
            return Activator.CreateInstance(wrapperType)!;
        });

        return wrapper.Handle(request, _provider, cancellationToken);
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

internal abstract class RequestHandlerWrapperBase<TResponse>
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken);
}

internal class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var handler = provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.Handle((TRequest)request, cancellationToken);

        foreach (var behavior in behaviors.Reverse())
        {
            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle((TRequest)request, next, cancellationToken);
        }

        return handlerDelegate();
    }
}
