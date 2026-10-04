using Ardalis.Result;
using StackDuel.Application.Queries;

namespace __NAMESPACE__;

internal sealed class SampleHandler(/* TODO: inject repository */) : IQueryHandler<SampleQuery, __RESPONSE__>
{
    public async ValueTask<Result<__RESPONSE__>> Handle(SampleQuery query, CancellationToken cancellationToken)
    {
        // TODO: implement
        throw new NotImplementedException();
    }
}
