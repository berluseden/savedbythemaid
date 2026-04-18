namespace SavedByTheMaid.Application.DTOs.Orders;

public record OrderDetailDto : OrderSummaryDto
{
    public string? State { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Tax { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public string? SpecialInstructions { get; init; }
    public List<OrderRoomDto> Rooms { get; init; } = [];
    public List<OrderAdditionalServiceDto> AdditionalServices { get; init; } = [];
    public List<MeetingSummaryDto> Meetings { get; init; } = [];
}

public record OrderRoomDto
{
    public string RoomName { get; init; } = "";
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}

public record OrderAdditionalServiceDto
{
    public string ServiceName { get; init; } = "";
    public decimal Price { get; init; }
}
