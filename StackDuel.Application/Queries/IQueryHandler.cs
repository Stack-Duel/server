using Ardalis.Result;

namespace StackDuel.Application.Queries;

public interface IQueryHandler<TQuery, TResponse> : Mediator.IQueryHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;