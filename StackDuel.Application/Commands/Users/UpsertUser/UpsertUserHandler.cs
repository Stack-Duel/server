using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Application.Services.Users;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Events;
using StackDuel.Domain.Users.Factories;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Application.Commands.Users.UpsertUser;

internal sealed partial class UpsertUserHandler(
    IValidator<UpsertUserCommand> validator,
    IAggregateFactory<User, CreateUserParams> userFactory,
    IUsernameGeneratorService usernameGenerator,
    IUserWriteRepository userRepository,
    IDomainEventDispatcher domainEventDispatcher
) : AbstractCommandHandler<UpsertUserCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpsertUserCommand request,
        CancellationToken cancellationToken
    )
    {
        User? user = await userRepository.FindBySubAsync(request.Sub, cancellationToken);

        return user is null
            ? await CreateUserAsync(request, cancellationToken)
            : await UpdateUserAsync(user, request, cancellationToken);
    }

    private async Task<Result?> RejectIfUsernameTakenAsync(
        string? requestedUsername,
        Guid? currentUserId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(requestedUsername))
            return null;

        User? owner = await userRepository.FindByUsername(new Username(requestedUsername), cancellationToken);
        if (owner is null || owner.Id == currentUserId)
            return null;

        return Result.Invalid(new ValidationError("Username", "That username is already taken."));
    }

    private async Task<Result> CreateUserAsync(UpsertUserCommand request, CancellationToken cancellationToken)
    {
        Result? usernameTaken = await RejectIfUsernameTakenAsync(request.Username, null, cancellationToken);
        if (usernameTaken is not null)
            return usernameTaken;

        string username = string.IsNullOrWhiteSpace(request.Username) ? usernameGenerator.Generate() : request.Username;

        User newUser = userFactory.Create(
            new CreateUserParams(username, request.Sub, request.ImageUrl, request.Tenant)
        );
        newUser.UpdateBio(!string.IsNullOrWhiteSpace(request.Bio) ? new Bio(request.Bio) : null);

        if (!string.IsNullOrWhiteSpace(request.Username))
            newUser.CompleteSetup();

        bool inserted = await userRepository.TryAddBySubAsync(newUser, cancellationToken);

        if (!inserted)
            return await UpdateConcurrentlyProvisionedUserAsync(request, cancellationToken);

        await domainEventDispatcher.DispatchAsync(newUser.PopDomainEvents(), cancellationToken);

        if (request.LanguageIds is not null)
            await userRepository.SetLanguagePreferencesAsync(newUser.Id, request.LanguageIds, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> UpdateConcurrentlyProvisionedUserAsync(
        UpsertUserCommand request,
        CancellationToken cancellationToken
    )
    {
        User? winner = await userRepository.FindBySubAsync(request.Sub, cancellationToken);

        return winner is null
            ? Result.Error("Failed to provision the account.")
            : await UpdateUserAsync(winner, request, cancellationToken);
    }

    private async Task<Result> UpdateUserAsync(
        User user,
        UpsertUserCommand request,
        CancellationToken cancellationToken
    )
    {
        bool usernameChanged = !string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username.Value;

        if (usernameChanged)
        {
            Result? usernameTaken = await RejectIfUsernameTakenAsync(request.Username, user.Id, cancellationToken);
            if (usernameTaken is not null)
                return usernameTaken;

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

        await domainEventDispatcher.DispatchAsync(
            [new UserAccessContextChangedDomainEvent(user.Sub)],
            cancellationToken
        );

        return Result.Success();
    }
}