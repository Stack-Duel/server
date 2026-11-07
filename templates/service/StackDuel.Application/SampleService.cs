using MediatR;

namespace __NAMESPACE__;

public interface ISampleService
{
    // TODO: define service methods, e.g.:
    // Task<Ardalis.Result.Result<Guid>> CreateAsync(/* params */ CancellationToken cancellationToken);
}

internal sealed class SampleService(IMediator mediator) : ISampleService
{
    // TODO: implement methods by calling mediator.Send(new SomeCommand(...), cancellationToken)
}
