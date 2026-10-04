using StackDuel.Application.Events;
using StackDuel.Application.Services.Users;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Factories;
using StackDuel.Domain.Users.ValueObjects;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Users.UpsertUser;

internal sealed partial class UpsertUserHandler(
    IValidator<UpsertUserCommand> validator,
    IAggregateFactory<User, CreateUserParams> userFactory,
    IUsernameGeneratorService usernameGenerator,
    IUserWriteRepository userRepository,
    IDomainEventDispatcher domainEventDispatcher,
    UserContext userContext
) : AbstractCommandHandler<UpsertUserCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpsertUserCommand request,
        CancellationToken cancellationToken
    )
    {
        if (userContext.User is null)
            return await CreateUserAsync(request, cancellationToken);

        User? user = await userRepository.FindBySubAsync(request.Sub, cancellationToken);

        return user is null
            ? await CreateUserAsync(request, cancellationToken)
            : await UpdateUserAsync(user, request, cancellationToken);
    }

    private async Task<Result> CreateUserAsync(UpsertUserCommand request, CancellationToken cancellationToken)
    {
        string username = string.IsNullOrWhiteSpace(request.Username)
            ? usernameGenerator.Generate()
            : request.Username;

        User newUser = userFactory.Create(
            new CreateUserParams(username, request.Sub, request.ImageUrl, request.Tenant)
        );
        newUser.UpdateBio(!string.IsNullOrWhiteSpace(request.Bio) ? new Bio(request.Bio) : null);

        if (!string.IsNullOrWhiteSpace(request.Username))
            newUser.CompleteSetup();

        await userRepository.AddAsync(newUser, cancellationToken);
        await domainEventDispatcher.DispatchAsync(newUser.PopDomainEvents(), cancellationToken);

        if (request.LanguageIds is not null)
            await userRepository.SetLanguagePreferencesAsync(newUser.Id, request.LanguageIds, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> UpdateUserAsync(
        User user,
        UpsertUserCommand request,
        CancellationToken cancellationToken
    )
    {
        bool usernameChanged =
            !string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username.Value;

        if (usernameChanged)
        {
            if (
                user.UsernameLastChangedAt.HasValue
                && DateTime.UtcNow - user.UsernameLastChangedAt.Value
                    < TimeSpan.FromDays(User.MaxDaysUntilUsernameChange)
            )
                return Result.Invalid(
                    new ValidationError("Username", "Username can only be changed once every 30 days.")
                );

            user.ChangeUsername(new Username(request.Username!));
        }

        if (user.SetupCompletedAt is null && !string.IsNullOrWhiteSpace(request.Username))
            user.CompleteSetup();

        if (request.Bio is not null)
            user.UpdateBio(!string.IsNullOrWhiteSpace(request.Bio) ? new Bio(request.Bio) : null);

        if (user.ImageUrl is null && !string.IsNullOrWhiteSpace(request.ImageUrl))
            user.UpdateImageUrl(new ImageUrl(request.ImageUrl));

        await userRepository.UpdateAsync(user, cancellationToken);

        if (request.LanguageIds is not null)
            await userRepository.SetLanguagePreferencesAsync(user.Id, request.LanguageIds, cancellationToken);

        return Result.Success();
    }
}