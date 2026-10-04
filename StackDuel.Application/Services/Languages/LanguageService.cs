using StackDuel.Application.Languages.Dtos;
using StackDuel.Application.Queries.Languages.GetLanguages;
using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Services.Languages;

public interface ILanguageService
{
    Task<Result<IReadOnlyList<LanguageDto>>> GetAllAsync(CancellationToken cancellationToken);
}

internal sealed class LanguageService(IMediator mediator) : ILanguageService
{
    public async Task<Result<IReadOnlyList<LanguageDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLanguagesQuery(), cancellationToken);
        return result;
    }
}