using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests.Unit;

public class StatusHistoryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StatusHistoryService _sut;

    public StatusHistoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"StatusHistoryTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var logger = Mock.Of<ILogger<StatusHistoryService>>();
        _sut = new StatusHistoryService(_context, logger);
    }

    public void Dispose() => _context.Dispose();

    #region RecordOrderStatusChangeAsync

    [Fact]
    public async Task RecordOrderStatusChange_SavesHistory()
    {
        await _sut.RecordOrderStatusChangeAsync(
            serviceOrderId: 1,
            fromStatus: OrderStatus.Draft,
            toStatus: OrderStatus.Confirmed,
            changedById: "user-1",
            reasonCode: "CONFIRMED",
            notes: "Order confirmed by customer");

        var history = await _context.OrderStatusHistories.ToListAsync();
        history.Should().HaveCount(1);
        history[0].ServiceOrderId.Should().Be(1);
        history[0].FromStatus.Should().Be(OrderStatus.Draft);
        history[0].ToStatus.Should().Be(OrderStatus.Confirmed);
        history[0].ChangedById.Should().Be("user-1");
        history[0].ReasonCode.Should().Be("CONFIRMED");
        history[0].Notes.Should().Be("Order confirmed by customer");
        history[0].ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordOrderStatusChange_NullFromStatus_SavesSuccessfully()
    {
        await _sut.RecordOrderStatusChangeAsync(
            serviceOrderId: 1,
            fromStatus: null,
            toStatus: OrderStatus.Draft);

        var history = await _context.OrderStatusHistories.SingleAsync();
        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task RecordOrderStatusChange_NullOptionalFields_SavesSuccessfully()
    {
        await _sut.RecordOrderStatusChangeAsync(
            serviceOrderId: 1,
            fromStatus: OrderStatus.Confirmed,
            toStatus: OrderStatus.Cancelled);

        var history = await _context.OrderStatusHistories.SingleAsync();
        history.ChangedById.Should().BeNull();
        history.ReasonCode.Should().BeNull();
        history.Notes.Should().BeNull();
    }

    #endregion

    #region RecordMeetStatusChangeAsync

    [Fact]
    public async Task RecordMeetStatusChange_SavesHistory()
    {
        await _sut.RecordMeetStatusChangeAsync(
            serviceMeetId: 10,
            fromStatus: MeetStatus.Scheduled,
            toStatus: MeetStatus.InProgress,
            changedById: "employee-1");

        var history = await _context.MeetStatusHistories.ToListAsync();
        history.Should().HaveCount(1);
        history[0].ServiceMeetId.Should().Be(10);
        history[0].FromStatus.Should().Be(MeetStatus.Scheduled);
        history[0].ToStatus.Should().Be(MeetStatus.InProgress);
        history[0].ChangedById.Should().Be("employee-1");
    }

    [Fact]
    public async Task RecordMeetStatusChange_InitialCreation_NullFromStatus()
    {
        await _sut.RecordMeetStatusChangeAsync(
            serviceMeetId: 10,
            fromStatus: null,
            toStatus: MeetStatus.Scheduled);

        var history = await _context.MeetStatusHistories.SingleAsync();
        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(MeetStatus.Scheduled);
    }

    #endregion

    #region GetOrderStatusHistoryAsync

    [Fact]
    public async Task GetOrderStatusHistory_ReturnsOrderedByDateDesc()
    {
        await _sut.RecordOrderStatusChangeAsync(1, null, OrderStatus.Draft);
        await _sut.RecordOrderStatusChangeAsync(1, OrderStatus.Draft, OrderStatus.Confirmed);
        await _sut.RecordOrderStatusChangeAsync(1, OrderStatus.Confirmed, OrderStatus.Cancelled);

        var history = await _sut.GetOrderStatusHistoryAsync(1);

        history.Should().HaveCount(3);
        history[0].ToStatus.Should().Be(OrderStatus.Cancelled);
        history[2].ToStatus.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task GetOrderStatusHistory_DifferentOrders_ReturnsOnlyRequested()
    {
        await _sut.RecordOrderStatusChangeAsync(1, null, OrderStatus.Draft);
        await _sut.RecordOrderStatusChangeAsync(2, null, OrderStatus.Confirmed);

        var history = await _sut.GetOrderStatusHistoryAsync(1);
        history.Should().HaveCount(1);
        history[0].ServiceOrderId.Should().Be(1);
    }

    [Fact]
    public async Task GetOrderStatusHistory_NoHistory_ReturnsEmpty()
    {
        var history = await _sut.GetOrderStatusHistoryAsync(999);
        history.Should().BeEmpty();
    }

    #endregion

    #region GetMeetStatusHistoryAsync

    [Fact]
    public async Task GetMeetStatusHistory_ReturnsOrderedByDateDesc()
    {
        await _sut.RecordMeetStatusChangeAsync(1, null, MeetStatus.Scheduled);
        await _sut.RecordMeetStatusChangeAsync(1, MeetStatus.Scheduled, MeetStatus.InProgress);
        await _sut.RecordMeetStatusChangeAsync(1, MeetStatus.InProgress, MeetStatus.Completed);

        var history = await _sut.GetMeetStatusHistoryAsync(1);

        history.Should().HaveCount(3);
        history[0].ToStatus.Should().Be(MeetStatus.Completed);
        history[2].ToStatus.Should().Be(MeetStatus.Scheduled);
    }

    [Fact]
    public async Task GetMeetStatusHistory_NoHistory_ReturnsEmpty()
    {
        var history = await _sut.GetMeetStatusHistoryAsync(999);
        history.Should().BeEmpty();
    }

    #endregion
}
