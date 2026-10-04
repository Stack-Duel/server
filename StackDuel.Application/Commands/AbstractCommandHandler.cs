using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands;

public abstract class AbstractCommandHandler<TCommand, TResult>(IValidator<TCommand>? validator = null)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly IValidator<TCommand>? _validator = validator;

    public async Task<Result<TResult>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        if (_validator is not null)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult
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

                return Result<TResult>.Invalid(errors);
            }
        }

        return await HandleValidated(request, cancellationToken);
    }

    protected abstract Task<Result<TResult>> HandleValidated(TCommand request, CancellationToken cancellationToken);
}

public abstract class AbstractCommandHandler<TCommand>(IValidator<TCommand>? validator = null)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly IValidator<TCommand>? _validator = validator;

    public async Task<Result> Handle(TCommand request, CancellationToken cancellationToken)
    {
        if (_validator is not null)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult
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

                return Result.Invalid(errors);
            }
        }

        return await HandleValidated(request, cancellationToken);
    }

    protected abstract Task<Result> HandleValidated(TCommand request, CancellationToken cancellationToken);
}