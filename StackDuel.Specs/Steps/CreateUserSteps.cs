using Ardalis.Result;
using FluentValidation;
using Reqnroll;
using StackDuel.Application.Commands.Users.CreateUser;
using StackDuel.Domain.User.Entities;
using StackDuel.Domain.User.ValueObjects;
using StackDuel.Specs.Support;

namespace StackDuel.Specs.Steps;

[Binding]
public sealed class CreateUserSteps
{
    private readonly FakeUserRepository _userRepository = new();
    private Result<Guid> _result = null!;

    [Given(@"no user exists with sub ""(.*)""")]
    public void GivenNoUserExistsWithSub(string sub)
    {
        // Nothing to arrange: the fake repository starts empty.
    }

    [Given(@"a user already exists with sub ""(.*)""")]
    public void GivenAUserAlreadyExistsWithSub(string sub)
    {
        var user = new User(new Username("existing_user"), new UserSub(sub));
        _userRepository.Seed(user);
    }

    [When(@"I create a user with username ""(.*)"" and sub ""(.*)""")]
    public async Task WhenICreateAUserWithUsernameAndSub(string username, string sub)
    {
        var command = new CreateUserCommand(username, sub);
        var validator = new CreateUserValidator();
        var handler = new CreateUserHandler(validator, _userRepository);

        _result = await handler.Handle(command, CancellationToken.None);
    }

    [Then("the user is created successfully")]
    public void ThenTheUserIsCreatedSuccessfully()
    {
        Assert.True(_result.IsSuccess, string.Join("; ", _result.Errors));
        Assert.NotEqual(Guid.Empty, _result.Value);
    }

    [Then(@"the request fails with an error containing ""(.*)""")]
    public void ThenTheRequestFailsWithAnErrorContaining(string expectedSubstring)
    {
        Assert.False(_result.IsSuccess);
        var allErrors = _result.Errors.Concat(_result.ValidationErrors.Select(e => e.ErrorMessage));
        Assert.Contains(allErrors, error => error.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase));
    }
}