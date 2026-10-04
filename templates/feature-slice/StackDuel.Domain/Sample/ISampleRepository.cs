using __ROOT_NS__.Domain.SeedWork;

namespace __ROOT_NS__.Domain.Sample;

public interface ISampleRepository : IRepository<__AGGREGATE_NS__.Sample>
{
    // TODO: add aggregate-specific query methods beyond FindByIdAsync (inherited from IRepository<T>).
}
