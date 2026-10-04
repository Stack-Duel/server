using UpdateUserGroupsCommand = StackDuel.Application.Commands.Users.UpdateUserGroups.UpdateUserGroupsCommand;
using UpdateUserGroupsValidator = StackDuel.Application.Commands.Users.UpdateUserGroups.UpdateUserGroupsValidator;

namespace StackDuel.Application.Tests.Commands.Users.UpdateUserGroups;

public class UpdateUserGroupsValidatorTests
{
    private UpdateUserGroupsValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateUserGroupsValidator();
    }

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), [Guid.NewGuid()]);
        Assert.That(_validator.Validate(command).IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyUserId_IsInvalid()
    {
        var command = new UpdateUserGroupsCommand(Guid.Empty, [Guid.NewGuid()]);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_NullGroupIds_IsInvalid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), null!);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_GroupIdsContainsEmptyGuid_IsInvalid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), [Guid.Empty]);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyGroupIdsCollection_IsValid()
    {
        var command = new UpdateUserGroupsCommand(Guid.NewGuid(), []);
        Assert.That(_validator.Validate(command).IsValid, Is.True);
    }
}