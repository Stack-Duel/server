using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackDuel.Api.Controllers;
using StackDuel.Api.Requests.Audit;
using StackDuel.Application.Audit.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Services.Audit;

namespace StackDuel.Api.Tests.Controllers;

public class AuditLogControllerTests
{
    [Fact]
    public async Task GetAuditLog_ReturnsTheServicesPage()
    {
        PageResult<AuditLogEntryDto> page = ControllerAssert.Page<AuditLogEntryDto>();
        Mock<IAuditLogService> service = new();
        service
            .Setup(x => x.GetPageableAsync(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PageResult<AuditLogEntryDto>>.Success(page));

        ActionResult<PageResult<AuditLogEntryDto>> result = await ControllerAssert
            .WithHttpContext(new AuditLogController(service.Object))
            .GetAuditLog(new GetAuditLogPageableRequest(1, 20, DateTime.UnixEpoch), CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Same(page, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetAuditLog_ForwardsPageSizeAndTimestampFromTheQueryString()
    {
        DateTime timestamp = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        PaginationRequest? captured = null;
        Mock<IAuditLogService> service = new();
        service
            .Setup(x => x.GetPageableAsync(It.IsAny<PaginationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PaginationRequest, CancellationToken>((pagination, _) => captured = pagination)
            .ReturnsAsync(Result<PageResult<AuditLogEntryDto>>.Success(ControllerAssert.Page<AuditLogEntryDto>()));

        await ControllerAssert
            .WithHttpContext(new AuditLogController(service.Object))
            .GetAuditLog(new GetAuditLogPageableRequest(3, 50, timestamp), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(3, captured!.Page);
        Assert.Equal(50, captured.Size);
        Assert.Equal(timestamp, captured.Timestamp);
    }
}