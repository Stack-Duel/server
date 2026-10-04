using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.User;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Application.Commands.Users.CreateUser;

internal sealed class CreateUserHandler(IValidator<CreateUserCommand> validator, IUserRepository userRepository)
    : AbstractCommandHandler<CreateUserCommand, Guid>(validator)
{
    protected override async ValueTask<Result<Guid>> HandleValidated(
        CreateUserCommand command,
        CancellationToken cancellationToken
    )
    {
        var sub = new UserSub(command.Sub);

        if (await userRepository.FindBySubAsync(sub, cancellationToken) is not null)
            return Result<Guid>.Error("A user with this sub already exists.");

        var user = new Domain.User.Entities.User(new Username(command.Username), sub, command.ImageUrl, command.Tenant);

        await userRepository.AddAsync(user, cancellationToken);

        return Result.Success(user.Id);
    }
}