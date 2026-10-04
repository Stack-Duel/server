using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Application.Groups;
using StackDuel.Application.Groups.Dtos;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Events;

namespace StackDuel.Application.Commands.Users.UpdateUserGroups;

internal sealed partial class UpdateUserGroupsHandler(
    IValidator<UpdateUserGroupsCommand> validator,
    IUserWriteRepository userWriteRepository,
    IGroupReadRepository groupReadRepository,
    IDomainEventDispatcher domainEventDispatcher,
    UserContext userContext
) : AbstractCommandHandler<UpdateUserGroupsCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateUserGroupsCommand request,
        CancellationToken cancellationToken
    )
    {
        User? user = await userWriteRepository.FindByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound();

        IReadOnlyList<GroupDto> groups = await groupReadRepository.GetAllAsync(cancellationToken);
        HashSet<Guid> validGroupIds = [.. groups.Select(g => g.Id)];

        Guid[] unknownGroupIds = [.. request.GroupIds.Where(id => !validGroupIds.Contains(id)).Distinct()];

        if (unknownGroupIds.Length > 0)
            return Result.Invalid(
                new ValidationError("GroupIds", $"Unknown group id(s): {string.Join(", ", unknownGroupIds)}")
            );

        GroupDto? adminGroup = groups.FirstOrDefault(g => g.Name == WellKnownAuthorization.AdminGroup);

        if (adminGroup is not null && userContext.User?.Id != request.UserId)
        {
            IReadOnlyList<Guid> currentGroupIds = await userWriteRepository.GetGroupIdsAsync(
                request.UserId,
                cancellationToken
            );

            bool targetIsAdmin = currentGroupIds.Contains(adminGroup.Id);
            bool wouldRemainAdmin = request.GroupIds.Contains(adminGroup.Id);

            if (targetIsAdmin && !wouldRemainAdmin)
                return Result.Forbidden();
        }

        await userWriteRepository.SetGroupsAsync(request.UserId, request.GroupIds, cancellationToken);

        await domainEventDispatcher.DispatchAsync(
            [new UserAccessContextChangedDomainEvent(user.Sub)],
            cancellationToken
        );

        return Result.Success();
    }
}