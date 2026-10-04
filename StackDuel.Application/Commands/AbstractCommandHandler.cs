using Ardalis.Result;
using FluentValidation;
using FluentValidation.Results;

namespace StackDuel.Application.Commands;

public abstract class AbstractCommandHandler<TCommand, TResult>(IValidator<TCommand>? validator = null)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async ValueTask<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
                return Result<TResult>.Invalid(ValidationErrorMapper.Map(validationResult));
        }

        return await HandleValidated(command, cancellationToken);
    }

    protected abstract ValueTask<Result<TResult>> HandleValidated(TCommand command, CancellationToken cancellationToken);
}

public abstract class AbstractCommandHandler<TCommand>(IValidator<TCommand>? validator = null) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async ValueTask<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
                return Result.Invalid(ValidationErrorMapper.Map(validationResult));
        }

        return await HandleValidated(command, cancellationToken);
    }

    protected abstract ValueTask<Result> HandleValidated(TCommand command, CancellationToken cancellationToken);
}

internal static class ValidationErrorMapper
{
    public static List<ValidationError> Map(ValidationResult validationResult) =>
        validationResult
            .Errors.Select(e =>
            {
                string identifier = e.FormattedMessagePlaceholderValues.TryGetValue(
                    "PropertyName",
                    out object? displayName
                )
                    ? displayName?.ToString() ?? e.PropertyName
                    : e.PropertyName;

                return new ValidationError(identifier, e.ErrorMessage);
            })
            .ToList();
}