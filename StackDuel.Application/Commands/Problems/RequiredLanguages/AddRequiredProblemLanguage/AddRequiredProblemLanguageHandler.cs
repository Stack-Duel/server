using StackDuel.Domain.Problems.RequiredLanguages;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Problems.RequiredLanguages.AddRequiredProblemLanguage;

internal sealed class AddRequiredProblemLanguageHandler(
    IValidator<AddRequiredProblemLanguageCommand> validator,
    IRequiredProblemLanguageRepository repository
) : AbstractCommandHandler<AddRequiredProblemLanguageCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        AddRequiredProblemLanguageCommand request,
        CancellationToken cancellationToken
    )
    {
        RequiredProblemLanguage? existing = await repository.FindByLanguageVersionIdAsync(
            request.LanguageVersionId,
            cancellationToken
        );

        if (existing is not null)
            return Result.Invalid(new ValidationError("LanguageVersionId", "This language is already required."));

        IReadOnlyList<RequiredProblemLanguage> all = await repository.GetAllOrderedAsync(cancellationToken);

        RequiredProblemLanguage required = RequiredProblemLanguage.Create(request.LanguageVersionId, all.Count);

        await repository.AddAsync(required, cancellationToken);

        return Result.Success(required.Id);
    }
}