using StackDuel.Application.Events;
using StackDuel.Application.Services.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Factories;
using Ardalis.Result;
using Moq;
using UpsertUserCommand = StackDuel.Application.Commands.Users.UpsertUser.UpsertUserCommand;
using UpsertUserHandler = StackDuel.Application.Commands.Users.UpsertUser.UpsertUserHandler;
using UpsertUserValidator = StackDuel.Application.Commands.Users.UpsertUser.UpsertUserValidator;

namespace StackDuel.Application.Tests.Commands.Users.UpsertUser;

public class UpsertUserHandlerTests
{
    private const string ValidSub = "auth0|abc123";

    private Mock<IUserWriteRepository> _userRepository = null!;
    private Mock<IUsernameGeneratorService> _usernameGenerator = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private UserContext _userContext = null!;
    private UpsertUserHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserWriteRepository>();
        _usernameGenerator = new Mock<IUsernameGeneratorService>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _userContext = new UserContext();

        _handler = new UpsertUserHandler(
            new UpsertUserValidator(),
            new UserFactory(),
            _usernameGenerator.Object,
            _userRepository.Object,
            _domainEventDispatcher.Object,
            _userContext
        );
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Sub, user.Username.Value, null, null, false, null, user.CreatedAt, null, []);

    [Test]
    public async Task Handle_NewUser_NoUsernameProvided_GeneratesUsername()
    {
        _userRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _usernameGenerator.Setup(x => x.Generate()).Returns("generated_name42");

        var command = new UpsertUserCommand(ValidSub, null, null, null);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _userRepository.Verify(
            x => x.AddAsync(It.Is<User>(u => u.Username.Value == "generated_name42"), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_NewUser_NoUsernameProvided_DoesNotCompleteSetup()
    {
        _userRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _usernameGenerator.Setup(x => x.Generate()).Returns("generated_name42");

        User? createdUser = null;
        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        var command = new UpsertUserCommand(ValidSub, null, null, null);
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(createdUser!.SetupCompletedAt, Is.Null);
    }

    [Test]
    public async Task Handle_NewUser_WithUsername_CompletesSetup()
    {
        _userRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        User? createdUser = null;
        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        var command = new UpsertUserCommand(ValidSub, "alice", null, null);
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(createdUser!.SetupCompletedAt, Is.Not.Null);
    }

    [Test]
    public async Task Handle_NewUser_DispatchesDomainEvents()
    {
        _userRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var command = new UpsertUserCommand(ValidSub, "alice", null, null);
        await _handler.Handle(command, CancellationToken.None);

        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_NewUser_WithLanguageIds_SetsLanguagePreferences()
    {
        _userRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var languageIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new UpsertUserCommand(ValidSub, "alice", null, null, languageIds);
        await _handler.Handle(command, CancellationToken.None);

        _userRepository.Verify(
            x => x.SetLanguagePreferencesAsync(It.IsAny<Guid>(), languageIds, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_NewUser_WithTenant_SetsTenant()
    {
        _userRepository.Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        User? createdUser = null;
        _userRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        var command = new UpsertUserCommand(ValidSub, "alice", null, null, Tenant: "stackduel");
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(createdUser!.Tenant, Is.EqualTo("stackduel"));
    }

    [Test]
    public async Task Handle_ExistingUser_DoesNotOverwriteTenant()
    {
        var existingUser = new UserFactory().Create(new CreateUserParams("bob", ValidSub, null, "stackduel"));
        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(ValidSub, "alice", null, null, Tenant: "some-other-tenant");
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(existingUser.Tenant, Is.EqualTo("stackduel"));
    }

    [Test]
    public async Task Handle_ExistingUser_ChangesUsername()
    {
        var existingUser = new UserFactory().Create(new CreateUserParams("bob", ValidSub, null));
        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(ValidSub, "alice", null, null);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(existingUser.Username.Value, Is.EqualTo("alice"));
    }

    [Test]
    public async Task Handle_ExistingUser_UsernameChangeWithinCooldown_ReturnsInvalid()
    {
        var existingUser = new UserFactory().Create(new CreateUserParams("bob", ValidSub, null));
        existingUser.ChangeUsername(new Domain.Users.ValueObjects.Username("charlie"));

        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(ValidSub, "dave", null, null);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _userRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_ExistingUser_UpdatesBioAndImageUrl()
    {
        var existingUser = new UserFactory().Create(new CreateUserParams("bob", ValidSub, null));
        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(
            ValidSub,
            null,
            "https://example.com/avatar.png",
            "Competitive programmer."
        );
        await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(existingUser.Bio!.Value, Is.EqualTo("Competitive programmer."));
            Assert.That(existingUser.ImageUrl!.Value, Is.EqualTo("https://example.com/avatar.png"));
        });
        _userRepository.Verify(x => x.UpdateAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_ExistingUser_NullBioOnUpsert_DoesNotClearBio()
    {
        var existingUser = new UserFactory().Create(new CreateUserParams("bob", ValidSub, null));
        existingUser.UpdateBio(new Domain.Users.ValueObjects.Bio("Competitive programmer."));
        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(ValidSub, null, null, null);
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(existingUser.Bio!.Value, Is.EqualTo("Competitive programmer."));
    }

    [Test]
    public async Task Handle_ExistingUser_EmptyBioOnUpsert_ClearsBio()
    {
        var existingUser = new UserFactory().Create(new CreateUserParams("bob", ValidSub, null));
        existingUser.UpdateBio(new Domain.Users.ValueObjects.Bio("Competitive programmer."));
        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(ValidSub, null, null, "");
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(existingUser.Bio, Is.Null);
    }

    [Test]
    public async Task Handle_ExistingUser_WithImageUrl_NullImageUrlOnUpsert_DoesNotClearImage()
    {
        var existingUser = new UserFactory().Create(
            new CreateUserParams("bob", ValidSub, "https://example.com/avatar.png")
        );
        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(ValidSub, null, null, null);
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(existingUser.ImageUrl!.Value, Is.EqualTo("https://example.com/avatar.png"));
    }

    [Test]
    public async Task Handle_ExistingUser_WithImageUrl_DifferentImageUrlOnUpsert_DoesNotOverwriteImage()
    {
        var existingUser = new UserFactory().Create(
            new CreateUserParams("bob", ValidSub, "https://example.com/avatar.png")
        );
        _userRepository
            .Setup(x => x.FindBySubAsync(ValidSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userContext.User = ToDto(existingUser);

        var command = new UpsertUserCommand(ValidSub, null, "https://example.com/other.png", null);
        await _handler.Handle(command, CancellationToken.None);

        Assert.That(existingUser.ImageUrl!.Value, Is.EqualTo("https://example.com/avatar.png"));
    }

    [Test]
    public async Task Handle_InvalidCommand_ReturnsValidationErrorAndDoesNotTouchRepository()
    {
        var command = new UpsertUserCommand(ValidSub, null, null, new string('a', 1000));

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _userRepository.Verify(x => x.FindBySubAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}