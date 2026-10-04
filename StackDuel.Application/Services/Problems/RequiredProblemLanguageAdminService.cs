using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.Problems.RequiredLanguages.AddRequiredProblemLanguage;
using StackDuel.Application.Commands.Problems.RequiredLanguages.RemoveRequiredProblemLanguage;
using StackDuel.Application.Problems.RequiredLanguages.Dtos;
using StackDuel.Application.Queries.Problems.GetRequiredProblemLanguages;

namespace StackDuel.Application.Services.Problems;

public interface IRequiredProblemLanguageAdminService
{
    Task<Result<IReadOnlyList<RequiredProblemLanguageAdminDto>>> GetAllAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> AddAsync(Guid languageVersionId, CancellationToken cancellationToken);

    Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken);
}

internal sealed class RequiredProblemLanguageAdminService(IMediator mediator) : IRequiredProblemLanguageAdminService
{
    public async Task<Result<IReadOnlyList<RequiredProblemLanguageAdminDto>>> GetAllAsync(
        CancellationToken cancellationToken
    ) => await mediator.Send(new GetRequiredProblemLanguagesQuery(), cancellationToken);

    public async Task<Result<Guid>> AddAsync(Guid languageVersionId, CancellationToken cancellationToken) =>
        await mediator.Send(new AddRequiredProblemLanguageCommand(languageVersionId), cancellationToken);

    public async Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken) =>
        await mediator.Send(new RemoveRequiredProblemLanguageCommand(id), cancellationToken);
}