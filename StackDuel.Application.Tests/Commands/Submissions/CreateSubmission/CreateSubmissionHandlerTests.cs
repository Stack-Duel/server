using Ardalis.Result;
using Moq;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;
using CreateSubmissionCommand = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionCommand;
using CreateSubmissionHandler = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionHandler;
using CreateSubmissionValidator = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionValidator;

namespace StackDuel.Application.Tests.Commands.Submissions.CreateSubmission;

public class CreateSubmissionHandlerTests
{
    private readonly Mock<ISubmissionCreator> _submissionCreator = new();
    private readonly CreateSubmissionHandler _handler;

    public CreateSubmissionHandlerTests()
    {
        _handler = new CreateSubmissionHandler(new CreateSubmissionValidator(), _submissionCreator.Object);
    }

    private static CreateSubmissionCommand ValidCommand(
        IReadOnlyCollection<CreateSubmissionCustomTestCaseDto>? customTestCases = null,
        Guid? gameId = null
    ) => new(Guid.NewGuid(), SubmissionType.Run, "return 42;", Guid.NewGuid(), customTestCases, GameId: gameId);

    [Fact]
    public async Task Handle_ValidCommand_PassesEveryFieldToTheSubmissionCreator()
    {
        var customTestCases = new List<CreateSubmissionCustomTestCaseDto> { new(["1", "2"]) };
        CreateSubmissionCommand command = ValidCommand(customTestCases, gameId: Guid.NewGuid());
        var submissionId = Guid.NewGuid();

        _submissionCreator
            .Setup(x => x.CreateAsync(It.IsAny<SubmissionCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(submissionId));

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(submissionId, result.Value);
        _submissionCreator.Verify(
            x =>
                x.CreateAsync(
                    It.Is<SubmissionCreationRequest>(request =>
                        request.ProblemSetupId == command.ProblemSetupId
                        && request.Type == command.Type
                        && request.Code == command.Code
                        && request.CreatedById == command.CreatedById
                        && request.CustomTestCases == command.CustomTestCases
                        && request.AdditionalFiles == command.AdditionalFiles
                        && request.GameId == command.GameId
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CreatorFails_ReturnsThatResult()
    {
        _submissionCreator
            .Setup(x => x.CreateAsync(It.IsAny<SubmissionCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.NotFound("Submission pipeline is not available."));

        Result<Guid> result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndNeverCreatesASubmission()
    {
        CreateSubmissionCommand command = ValidCommand() with { Code = "" };

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _submissionCreator.Verify(
            x => x.CreateAsync(It.IsAny<SubmissionCreationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}