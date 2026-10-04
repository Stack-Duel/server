using Ardalis.Result;

namespace StackDuel.Application.Commands;

public interface ICommandHandler<TCommand, TResponse> : Mediator.ICommandHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface ICommandHandler<TCommand> : Mediator.ICommandHandler<TCommand, Result>
    where TCommand : ICommand;