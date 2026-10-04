using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Tests.SeedWork;

public class AggregateRootTests
{
    private readonly TestAggregateRoot _sut = new();

    private sealed class TestDomainEvent : IDomainEvent;

    private sealed class TestAggregateRoot : AggregateRoot
    {
        public void RaiseDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_AddsEventToDomainEvents()
    {
        var domainEvent = new TestDomainEvent();

        _sut.RaiseDomainEvent(domainEvent);

        var domainEvents = Assert.Single(_sut.DomainEvents);
        Assert.Same(domainEvent, domainEvents);
    }

    [Fact]
    public void AddDomainEvent_CalledMultipleTimes_AddsEachEvent()
    {
        var firstEvent = new TestDomainEvent();
        var secondEvent = new TestDomainEvent();

        _sut.RaiseDomainEvent(firstEvent);
        _sut.RaiseDomainEvent(secondEvent);

        Assert.Equal(2, _sut.DomainEvents.Count);
        Assert.Contains(firstEvent, _sut.DomainEvents);
        Assert.Contains(secondEvent, _sut.DomainEvents);
    }

    [Fact]
    public void PopDomainEvents_ReturnsAddedEventsAndClearsThem()
    {
        var domainEvent = new TestDomainEvent();
        _sut.RaiseDomainEvent(domainEvent);

        var poppedEvents = _sut.PopDomainEvents();

        var poppedEvent = Assert.Single(poppedEvents);
        Assert.Same(domainEvent, poppedEvent);
        Assert.Empty(_sut.DomainEvents);
    }

    [Fact]
    public void PopDomainEvents_CalledTwice_SecondCallReturnsEmpty()
    {
        _sut.RaiseDomainEvent(new TestDomainEvent());

        _sut.PopDomainEvents();
        var secondPop = _sut.PopDomainEvents();

        Assert.Empty(secondPop);
    }

    [Fact]
    public void PopDomainEvents_WhenNoEventsAdded_ReturnsEmpty()
    {
        var poppedEvents = _sut.PopDomainEvents();

        Assert.Empty(poppedEvents);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void AddDomainEvent_AddsExpectedNumberOfEvents(int eventCount)
    {
        for (int i = 0; i < eventCount; i++)
            _sut.RaiseDomainEvent(new TestDomainEvent());

        Assert.Equal(eventCount, _sut.DomainEvents.Count);
    }
}