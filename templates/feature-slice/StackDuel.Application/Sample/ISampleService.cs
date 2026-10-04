using Ardalis.Result;
using __ROOT_NS__.Application.Sample.Dtos;

namespace __ROOT_NS__.Application.Sample;

public interface ISampleService
{
    Task<Result<Guid>> CreateAsync(/* TODO: params */ CancellationToken cancellationToken);

    Task<Result<SampleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
