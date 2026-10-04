using Ardalis.Result;
using FluentValidation;
using __ROOT_NS__.Application.Commands;
using __ROOT_NS__.Domain.Sample;

namespace __ROOT_NS__.Application.Commands.Sample.CreateSample;

internal sealed class CreateSampleHandler(IValidator<CreateSampleCommand> validator, ISampleRepository repository)
    : AbstractCommandHandler<CreateSampleCommand, Guid>(validator)
{
    protected override async ValueTask<Result<Guid>> HandleValidated(
        CreateSampleCommand command,
        CancellationToken cancellationToken
    )
    {
        // TODO: construct the __AGGREGATE_NS__.Sample aggregate and persist it, e.g.:
        // var entity = new __AGGREGATE_NS__.Sample(...);
        // await repository.AddAsync(entity, cancellationToken);
        // return Result.Success(entity.Id);
        throw new NotImplementedException();
    }
}
