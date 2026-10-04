using Ardalis.Result;

namespace StackDuel.Application.Commands;

public interface ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>;

public interface ICommand : Mediator.ICommand<Result>;