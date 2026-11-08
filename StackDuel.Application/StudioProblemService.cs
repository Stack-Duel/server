using Ardalis.Result;
using MediatR;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemSetups;
using StackDuel.Application.StudioProblems.Dtos;

namespace StackDuel.Application;

public interface IStudioProblemService
{
    Task<Result<StudioProblemDetailsDto>> GetStudioProblemByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Result<IEnumerable<StudioProblemDetailsSetupDto>>> GetStudioProblemDetailSetupsAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    );
}

internal sealed class StudioProblemService(IMediator mediator) : IStudioProblemService
{
    public async Task<Result<StudioProblemDetailsDto>> GetStudioProblemByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetStudioProblemByIdQuery(id, userId), cancellationToken);

    public async Task<Result<IEnumerable<StudioProblemDetailsSetupDto>>> GetStudioProblemDetailSetupsAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetStudioProblemSetupsQuery(id, userId), cancellationToken);
}