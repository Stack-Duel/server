using FluentValidation;
using StackDuel.Application.Commands;
using System.Reflection;

namespace StackDuel.ArchitectureTests;

public class CommandValidationTests
{
    private static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;

    private static readonly IReadOnlySet<string> CommandsAuthorizedWithoutAValidator = new HashSet<string>
    {
        "ReceiveJudge0CallbackCommand",
    };

    [Fact]
    public void Every_Command_Should_Have_A_Validator()
    {
        IReadOnlyList<Type> validatedTypes =
        [
            .. ApplicationAssembly
                .GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .SelectMany(type => type.BaseType is { IsGenericType: true } ? new[] { type.BaseType } : [])
                .Where(baseType => baseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                .Select(baseType => baseType.GetGenericArguments()[0]),
        ];

        IReadOnlyList<Type> commandsWithoutValidator =
        [
            .. Commands().Where(command => !validatedTypes.Contains(command)),
        ];

        Assert.Empty(commandsWithoutValidator.Select(command => command.Name));
    }

    [Fact]
    public void Every_Command_Handler_Should_Accept_Its_Validator()
    {
        IReadOnlyList<string> handlersNotTakingAValidator =
        [
            .. ApplicationAssembly
                .GetTypes()
                .Where(IsCommandHandler)
                .Where(handler => !TakesAValidator(handler))
                .Select(handler => handler.Name),
        ];

        Assert.Empty(handlersNotTakingAValidator);
    }

    private static IEnumerable<Type> Commands() =>
        ApplicationAssembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract
                && !type.IsInterface
                && type.GetInterfaces()
                    .Any(contract =>
                        contract == typeof(ICommand)
                        || (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICommand<>))
                    )
            )
            .Where(type => !CommandsAuthorizedWithoutAValidator.Contains(type.Name));

    private static bool IsCommandHandler(Type type) =>
        !type.IsAbstract
        && !type.IsInterface
        && type.GetInterfaces()
            .Any(contract =>
                contract.IsGenericType
                && (
                    contract.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)
                    || contract.GetGenericTypeDefinition() == typeof(ICommandHandler<>)
                )
            )
        && !HandlesACommandAuthorizedWithoutAValidator(type);

    private static bool HandlesACommandAuthorizedWithoutAValidator(Type handler) =>
        handler
            .GetInterfaces()
            .Where(contract => contract.IsGenericType)
            .SelectMany(contract => contract.GetGenericArguments())
            .Any(argument => CommandsAuthorizedWithoutAValidator.Contains(argument.Name));

    private static bool TakesAValidator(Type handler) =>
        handler
            .GetConstructors()
            .Any(constructor =>
                constructor
                    .GetParameters()
                    .Any(parameter =>
                        parameter.ParameterType.IsGenericType
                        && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IValidator<>)
                    )
            );
}