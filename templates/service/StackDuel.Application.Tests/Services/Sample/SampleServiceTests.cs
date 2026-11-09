using MediatR;
using Moq;
using __NAMESPACE__;

namespace __TEST_NAMESPACE__.Sample;

public class SampleServiceTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly SampleService _service;

    public SampleServiceTests()
    {
        _service = new SampleService(_mediator.Object);
    }

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // TODO: replace with real tests once SampleService has methods, e.g.:
        // await _service.DoSomethingAsync(..., CancellationToken.None);
        // _mediator.Verify(x => x.Send(It.IsAny<SomeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(_service);
    }
}
