using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Queries;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{ }