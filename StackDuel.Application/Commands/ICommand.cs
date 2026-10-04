using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Commands;

public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

public interface ICommand : IRequest<Result> { }