using StackDuel.Application.Services.Users;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Factories;
using StackDuel.Domain.Users.ValueObjects;
using Ardalis.Result;
using Moq;
using UploadUserAvatarCommand = StackDuel.Application.Commands.Users.UploadUserAvatar.UploadUserAvatarCommand;
using UploadUserAvatarHandler = StackDuel.Application.Commands.Users.UploadUserAvatar.UploadUserAvatarHandler;
using UploadUserAvatarValidator = StackDuel.Application.Commands.Users.UploadUserAvatar.UploadUserAvatarValidator;

namespace StackDuel.Application.Tests.Commands.Users.UploadUserAvatar;

public class UploadUserAvatarHandlerTests
{
    private static readonly byte[] ValidPngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] ValidJpegBytes = [0xFF, 0xD8, 0xFF, 0, 0, 0];
    private static readonly byte[] GifBytes = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0, 0, 0, 0];

    private Mock<IUserWriteRepository> _userRepository = null!;
    private Mock<IUserAvatarWriteRepository> _userAvatarRepository = null!;
    private Mock<IAvatarBlobStorage> _avatarBlobStorage = null!;
    private UploadUserAvatarHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserWriteRepository>();
        _userAvatarRepository = new Mock<IUserAvatarWriteRepository>();
        _avatarBlobStorage = new Mock<IAvatarBlobStorage>();

        _userAvatarRepository
            .Setup(x => x.GetAllByUserIdNewestFirstAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UserAvatar>)[]);

        _handler = new UploadUserAvatarHandler(
            new UploadUserAvatarValidator(),
            _userRepository.Object,
            _userAvatarRepository.Object,
            _avatarBlobStorage.Object
        );
    }

    [Test]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new UploadUserAvatarCommand(Guid.NewGuid(), ValidPngBytes);
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_ValidUpload_UploadsSetsImageUrlAndReturnsBlobUrl()
    {
        var user = new UserFactory().Create(new CreateUserParams("alice", "auth0|abc", null));
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _avatarBlobStorage
            .Setup(x =>
                x.UploadAsync(user.Id, It.IsAny<Guid>(), It.IsAny<byte[]>(), "image/png", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("https://storage.example.com/avatars/abc.png");

        var command = new UploadUserAvatarCommand(user.Id, ValidPngBytes);
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo("https://storage.example.com/avatars/abc.png"));
            Assert.That(user.ImageUrl!.Value, Is.EqualTo("https://storage.example.com/avatars/abc.png"));
        });
        _userAvatarRepository.Verify(
            x => x.AddAsync(It.IsAny<UserAvatar>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _userRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_JpegSignature_UploadsWithDetectedContentType()
    {
        var user = new UserFactory().Create(new CreateUserParams("alice", "auth0|abc", null));
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _avatarBlobStorage
            .Setup(x =>
                x.UploadAsync(
                    user.Id,
                    It.IsAny<Guid>(),
                    It.IsAny<byte[]>(),
                    "image/jpeg",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("https://storage.example.com/avatars/abc.jpg");

        var command = new UploadUserAvatarCommand(user.Id, ValidJpegBytes);
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _avatarBlobStorage.Verify(
            x =>
                x.UploadAsync(
                    user.Id,
                    It.IsAny<Guid>(),
                    It.IsAny<byte[]>(),
                    "image/jpeg",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_MoreThanThreeAvatars_PrunesOldestBeyondThree()
    {
        var user = new UserFactory().Create(new CreateUserParams("alice", "auth0|abc", null));
        _userRepository.Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _avatarBlobStorage
            .Setup(x =>
                x.UploadAsync(
                    user.Id,
                    It.IsAny<Guid>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("https://storage.example.com/avatars/new.png");

        var newest = new UserAvatar(user.Id, new ImageUrl("https://storage.example.com/avatars/1.png"));
        var second = new UserAvatar(user.Id, new ImageUrl("https://storage.example.com/avatars/2.png"));
        var third = new UserAvatar(user.Id, new ImageUrl("https://storage.example.com/avatars/3.png"));
        var oldest = new UserAvatar(user.Id, new ImageUrl("https://storage.example.com/avatars/4.png"));

        _userAvatarRepository
            .Setup(x => x.GetAllByUserIdNewestFirstAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UserAvatar>)[newest, second, third, oldest]);

        var command = new UploadUserAvatarCommand(user.Id, ValidPngBytes);
        await _handler.Handle(command, CancellationToken.None);

        _avatarBlobStorage.Verify(x => x.DeleteAsync(oldest.ImageUrl.Value, It.IsAny<CancellationToken>()), Times.Once);
        _userAvatarRepository.Verify(x => x.DeleteAsync(oldest, It.IsAny<CancellationToken>()), Times.Once);
        _userAvatarRepository.Verify(x => x.DeleteAsync(newest, It.IsAny<CancellationToken>()), Times.Never);
        _userAvatarRepository.Verify(x => x.DeleteAsync(second, It.IsAny<CancellationToken>()), Times.Never);
        _userAvatarRepository.Verify(x => x.DeleteAsync(third, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_UnrecognizedFileSignature_ReturnsInvalidAndDoesNotUpload()
    {
        var command = new UploadUserAvatarCommand(Guid.NewGuid(), GifBytes);
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _avatarBlobStorage.Verify(
            x =>
                x.UploadAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_SpoofedSignature_ReturnsInvalidAndDoesNotUpload()
    {
        byte[] htmlDisguisedAsBytes = "<script>alert(1)</script>"u8.ToArray();
        var command = new UploadUserAvatarCommand(Guid.NewGuid(), htmlDisguisedAsBytes);
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _avatarBlobStorage.Verify(
            x =>
                x.UploadAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_ContentTooLarge_ReturnsInvalid()
    {
        byte[] content = new byte[UploadUserAvatarValidator.MaxContentBytes + 1];
        ValidPngBytes.CopyTo(content, 0);

        var command = new UploadUserAvatarCommand(Guid.NewGuid(), content);
        Result<string> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }
}