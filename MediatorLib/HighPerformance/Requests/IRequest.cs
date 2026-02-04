namespace MediatorLib.HighPerformance.Requests;

public interface IRequest<TResponse>
{
    Task<TResponse> SendTo(Mediator mediator, CancellationToken cancellationToken);
}
