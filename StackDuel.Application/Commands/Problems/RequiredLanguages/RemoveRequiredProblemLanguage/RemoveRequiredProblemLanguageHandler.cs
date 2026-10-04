using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Problems.RequiredLanguages;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;

namespace StackDuel.Application.Commands.Problems.RequiredLanguages.RemoveRequiredProblemLanguage;

internal sealed class RemoveRequiredProblemLanguageHandler(
    IValidator<RemoveRequiredProblemLanguageCommand> validator,
    IRequiredProblemLanguageRepository repository
) : AbstractCommandHandler<RemoveRequiredProblemLanguageCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        RemoveRequiredProblemLanguageCommand request,
        CancellationToken cancellationToken
    )
    {
        RequiredProblemLanguage? existing = await repository.FindByIdAsync(request.Id, cancellationToken);

        if (existing is null)
            return Result.NotFound();

        await repository.RemoveAsync(request.Id, cancellationToken);

        return Result.Success();
    }
}