using Ardalis.Result;
using __ROOT_NS__.Application.Queries;
using __ROOT_NS__.Application.Sample.Dtos;
using __ROOT_NS__.Domain.Sample;

namespace __ROOT_NS__.Application.Queries.Sample.GetSampleById;

internal sealed class GetSampleByIdHandler(ISampleRepository repository) : IQueryHandler<GetSampleByIdQuery, SampleDto>
{
    public async ValueTask<Result<SampleDto>> Handle(GetSampleByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await repository.FindByIdAsync(query.Id, cancellationToken);

        if (entity is null)
            return Result.NotFound();

        // TODO: map `entity` (__AGGREGATE_NS__.Sample) onto SampleDto.
        throw new NotImplementedException();
    }
}
