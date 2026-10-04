using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Queries;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }