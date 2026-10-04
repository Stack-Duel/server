using Ardalis.Result;

namespace StackDuel.Application.Queries;

public interface IQuery<TResponse> : Mediator.IQuery<Result<TResponse>>;