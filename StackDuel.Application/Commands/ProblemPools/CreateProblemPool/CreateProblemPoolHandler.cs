using StackDuel.Application.Problems;
using StackDuel.Domain.Problems.Entities;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.ProblemPools.CreateProblemPool;

internal sealed class CreateProblemPoolHandler(
    IProblemPoolRepository problemPoolRepository,
    IValidator<CreateProblemPoolCommand> validator
) : AbstractCommandHandler<CreateProblemPoolCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateProblemPoolCommand request,
        CancellationToken cancellationToken
    )
    {
        string normalizedKey = request.Key.Trim().ToLowerInvariant();

        bool exists = await problemPoolRepository.ExistsByKeyAsync(normalizedKey, cancellationToken);
        if (exists)
            return Result<Guid>.Invalid(
                new ValidationError(nameof(request.Key), $"A pool with key '{normalizedKey}' already exists.")
            );

        ProblemPool pool = new(normalizedKey, request.Name, request.Description);

        await problemPoolRepository.AddAsync(pool, cancellationToken);

        return Result<Guid>.Success(pool.Id);
    }
}