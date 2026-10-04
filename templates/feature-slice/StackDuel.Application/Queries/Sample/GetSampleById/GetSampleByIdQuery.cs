using __ROOT_NS__.Application.Queries;
using __ROOT_NS__.Application.Sample.Dtos;

namespace __ROOT_NS__.Application.Queries.Sample.GetSampleById;

public sealed record GetSampleByIdQuery(Guid Id) : IQuery<SampleDto>;
