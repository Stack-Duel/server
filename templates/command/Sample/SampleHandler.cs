using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Commands;

namespace __NAMESPACE__;

internal sealed class SampleHandler(IValidator<SampleCommand> validator /* TODO: inject repository/services */)
    : AbstractCommandHandler<SampleCommand, __RESPONSE__>(validator)
{
    protected override async ValueTask<Result<__RESPONSE__>> HandleValidated(
        SampleCommand command,
        CancellationToken cancellationToken
    )
    {
        // TODO: implement
        throw new NotImplementedException();
    }
}
