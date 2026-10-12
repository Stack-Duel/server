using UpdateUserGroupsCommand = StackDuel.Application.Commands.Users.UpdateUserGroups.UpdateUserGroupsCommand;
using UpdateUserGroupsValidator = StackDuel.Application.Commands.Users.UpdateUserGroups.UpdateUserGroupsValidator;

namespace StackDuel.Application.Tests.Commands.Users.UpdateUserGroups;

public class UpdateUserGroupsValidatorTests
{
    private UpdateUserGroupsValidator _validator = null!;

    public UpdateUserGroupsValidatorTests()
    {
        _validator = new UpdateUserGroupsValidator();
    }

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), [Guid.NewGuid()]);
        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyUserId_IsInvalid()
    {
        var command = new UpdateUserGroupsCommand(Guid.Empty, [Guid.NewGuid()]);
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_NullGroupIds_IsInvalid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), null!);
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_GroupIdsContainsEmptyGuid_IsInvalid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), [Guid.Empty]);
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyGroupIdsCollection_IsValid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), []);
        Assert.True(_validator.Validate(command).IsValid);
    }
}