using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;

internal sealed class SetProblemGenerationParametersHandler(
    IValidator<SetProblemGenerationParametersCommand> validator,
    IProblemRepository problemRepository,
    IValueTypeDefinitionRepository valueTypeDefinitionRepository
) : AbstractCommandHandler<SetProblemGenerationParametersCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetProblemGenerationParametersCommand request,
        CancellationToken cancellationToken
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(request.ProblemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        if (problem.Status == ProblemStatus.Pending)
            return Result.Invalid(
                new ValidationError("Status", "Cannot edit a problem while validation is in progress.")
            );

        List<ValidationError> errors = await ValidateAgainstCatalog(request, cancellationToken);

        if (errors.Count > 0)
            return Result.Invalid(errors);

        problem.SetGenerationParameters(
            request.Parameters,
            request.OutputValueType,
            request.TargetCaseCount,
            request.Seed
        );

        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }

    private async Task<List<ValidationError>> ValidateAgainstCatalog(
        SetProblemGenerationParametersCommand request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ValueTypeDefinition> definitions = await valueTypeDefinitionRepository.GetAllOrderedAsync(
            cancellationToken
        );

        Dictionary<string, ValueTypeSpec> catalog = definitions.ToDictionary(
            definition => definition.Key.Value,
            definition => definition.Spec
        );
        string available = Describe(catalog.Keys.Order());

        List<ValidationError> errors = [];

        if (!catalog.ContainsKey(request.OutputValueType))
            errors.Add(UnknownValueType(nameof(request.OutputValueType), request.OutputValueType, available));

        foreach ((GenerationParameterSpec parameter, int index) in request.Parameters.Select((p, i) => (p, i)))
            errors.AddRange(ValidateParameter(parameter, index, catalog, available));

        return errors;
    }

    private static IEnumerable<ValidationError> ValidateParameter(
        GenerationParameterSpec parameter,
        int index,
        IReadOnlyDictionary<string, ValueTypeSpec> catalog,
        string available
    )
    {
        string field = $"Parameters[{index}]";

        if (!catalog.TryGetValue(parameter.ValueType, out ValueTypeSpec? structure))
        {
            yield return UnknownValueType(field, parameter.ValueType, available);
            yield break;
        }

        IReadOnlyList<string> unsupported = GenerationParameterFields.UnsupportedOverrides(parameter, structure);

        if (unsupported.Count == 0)
            yield break;

        IReadOnlyList<ValueTypeFieldDescriptor> accepted = GenerationParameterFields.AcceptedBy(structure);

        yield return new ValidationError(
            field,
            $"Parameter '{parameter.Name}' of type '{parameter.ValueType}' does not accept "
                + $"{Describe(unsupported)}. "
                + (
                    accepted.Count > 0
                        ? $"It accepts {Describe(accepted.Select(descriptor => descriptor.Name))}."
                        : "It accepts no constraints."
                )
        );
    }

    private static ValidationError UnknownValueType(string field, string valueType, string available) =>
        new(field, $"Unknown value type '{valueType}'. Available value types: {available}.");

    private static string Describe(IEnumerable<string> values) => string.Join(", ", values);
}