using Ardalis.Result;
using Mediator;
using __ROOT_NS__.Application.Commands.Sample.CreateSample;
using __ROOT_NS__.Application.Queries.Sample.GetSampleById;
using __ROOT_NS__.Application.Sample.Dtos;

namespace __ROOT_NS__.Application.Sample;

internal sealed class SampleService(ISender sender) : ISampleService
{
    public async Task<Result<Guid>> CreateAsync(/* TODO: params */ CancellationToken cancellationToken) =>
        await sender.Send(new CreateSampleCommand(/* TODO: map params */), cancellationToken);

    public async Task<Result<SampleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new GetSampleByIdQuery(id), cancellationToken);
}
