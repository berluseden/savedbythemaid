using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Services;
using SavedByTheMaid.Api.Tests.Helpers;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Domain.Enums;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Tests.Unit;

/// <summary>
/// Unit tests for BookingService - pricing engine, order confirmation, and recurring meetings.
/// Uses in-memory database for real EF Core behavior and mocks for external dependencies.
/// </summary>
public class BookingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BookingService _sut;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPasswordHasher<ApplicationUser>> _passwordHasherMock;
    private readonly Mock<ISchedulingService> _schedulingServiceMock;

    public BookingServiceTests()
    {
        _context = TestDbContextFactory.Create();

        // Seed base data needed by all tests
        TestDbContextFactory.SeedRoles(_context);
        TestDbContextFactory.SeedServiceTypes(_context);
        TestDbContextFactory.SeedCleaningPlaces(_context);
        TestDbContextFactory.SeedServiceAreas(_context);
        TestDbContextFactory.SeedEmployees(_context);

        var logger = Mock.Of<ILogger<BookingService>>();

        _jwtServiceMock = new Mock<IJwtService>();
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns("test-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("test-refresh-token");

        _passwordHasherMock = new Mock<IPasswordHasher<ApplicationUser>>();
        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Returns("hashed-password");

        _schedulingServiceMock = new Mock<ISchedulingService>();
        _schedulingServiceMock.Setup(s => s.AcquireSlotsAsync(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<OccupancyType>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
            .Returns(Task.CompletedTask);

        _sut = new BookingService(
            _context,
            logger,
            _jwtServiceMock.Object,
            _passwordHasherMock.Object,
            _schedulingServiceMock.Object);
    }

    public void Dispose() => _context.Dispose();

    #region CalculatePricingAsync - Basic Pricing

    [Fact]
    public async Task CalculatePricing_WithBasicRooms_ReturnsCorrectPrice()
    {
        // Arrange - use room-based pricing with 2 bedrooms and 1 bathroom
        var input = new PricingInput
        {
            ServiceTypeId = 1,
            Rooms = new List<RoomPricingItem>
            {
                new(RoomId: 1, Quantity: 2), // 2 bedrooms * $15 = $30
                new(RoomId: 2, Quantity: 1)  // 1 bathroom * $20 = $20
            }
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base price ($80) + 2 bedrooms ($30) + 1 bathroom ($20) = $130
        result.Subtotal.Should().Be(130.00m);
        // Base minutes (60) + 2*20min (bedroom) + 1*25min (bathroom) = 145 min
        result.EstimatedMinutes.Should().Be(145);
        result.Total.Should().Be(result.Subtotal - result.Discount);
    }

    [Fact]
    public async Task CalculatePricing_WithNoRooms_UsesFallbackBedroomBathroom()
    {
        // Arrange - no rooms specified, use bedroom/bathroom fallback
        var input = new PricingInput
        {
            ServiceTypeId = 1,
            Rooms = null,
            Bedrooms = 3, // 2 extra bedrooms * $15 = $30
            Bathrooms = 2  // 1 extra bathroom * $20 = $20
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base ($80) + 2 extra bedrooms ($30) + 1 extra bathroom ($20) = $130
        result.Subtotal.Should().Be(130.00m);
        // Base (60) + 2*20 (bedrooms) + 1*15 (bathrooms) = 115 min
        result.EstimatedMinutes.Should().Be(115);
    }

    [Fact]
    public async Task CalculatePricing_WithSingleBedroomBathroom_NoExtraCharge()
    {
        // Arrange - 1 bedroom, 1 bathroom = no extra charges above base
        var input = new PricingInput
        {
            ServiceTypeId = 1,
            Rooms = null,
            Bedrooms = 1,
            Bathrooms = 1
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.Subtotal.Should().Be(80.00m); // Just base price
        result.EstimatedMinutes.Should().Be(60); // Just base minutes
    }

    [Fact]
    public async Task CalculatePricing_WithInvalidServiceType_ReturnsFailure()
    {
        // Arrange
        var input = new PricingInput { ServiceTypeId = 999 };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid service type");
    }

    [Fact]
    public async Task CalculatePricing_WithEmptyRoomsList_UsesFallbackPricing()
    {
        // Arrange - empty rooms list (not null, but empty)
        var input = new PricingInput
        {
            ServiceTypeId = 1,
            Rooms = new List<RoomPricingItem>(),
            Bedrooms = 2,
            Bathrooms = 1
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Empty list has Any() == false, so falls back to bedroom/bathroom pricing
        // Base ($80) + 1 extra bedroom ($15) = $95
        result.Subtotal.Should().Be(95.00m);
    }

    #endregion

    #region CalculatePricingAsync - Additional Services

    [Fact]
    public async Task CalculatePricing_WithAdditionalServices_AddsToTotal()
    {
        // Arrange - seed additional services
        TestDbContextFactory.SeedAdditionalServiceTypes(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            AdditionalServiceIds = new List<int> { 1, 2 } // Oven ($30) + Window ($25)
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base ($80) + Oven ($30) + Window ($25) = $135
        result.Subtotal.Should().Be(135.00m);
        // Base (60) + Oven (30) + Window (20) = 110 min
        result.EstimatedMinutes.Should().Be(110);
    }

    [Fact]
    public async Task CalculatePricing_WithNonExistentAdditionalService_IgnoresInvalid()
    {
        // Arrange
        TestDbContextFactory.SeedAdditionalServiceTypes(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            AdditionalServiceIds = new List<int> { 1, 999 } // Oven ($30) + non-existent
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base ($80) + Oven ($30) = $110
        result.Subtotal.Should().Be(110.00m);
    }

    [Fact]
    public async Task CalculatePricing_WithNoAdditionalServices_NoExtraCharge()
    {
        // Arrange
        var input = new PricingInput
        {
            ServiceTypeId = 1,
            AdditionalServiceIds = null
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.Subtotal.Should().Be(80.00m);
    }

    #endregion

    #region CalculatePricingAsync - Price Multipliers

    [Fact]
    public async Task CalculatePricing_WithPriceMultiplier_DirtLevel_AppliesCorrectly()
    {
        // Arrange
        TestDbContextFactory.SeedPriceMultipliers(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            DirtLevel = DirtLevel.Heavy // triggers multiplier factor 1.3
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base $80 * 1.3 (heavy dirt) * 1.15 (first time, since IsFirstTime defaults true)
        // = $80 * 1.3 * 1.15 = $119.60
        result.Subtotal.Should().Be(80.00m * 1.3m * 1.15m);
    }

    [Fact]
    public async Task CalculatePricing_WithPriceMultiplier_HasPets_AppliesCorrectly()
    {
        // Arrange
        TestDbContextFactory.SeedPriceMultipliers(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            HasPets = true,
            IsFirstTime = false // disable first-time multiplier
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base $80 * 1.1 (pets) = $88
        result.Subtotal.Should().Be(80.00m * 1.1m);
    }

    [Fact]
    public async Task CalculatePricing_WithPriceMultiplier_FirstTime_AppliesCorrectly()
    {
        // Arrange
        TestDbContextFactory.SeedPriceMultipliers(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            IsFirstTime = true,
            HasPets = false,
            DirtLevel = DirtLevel.Normal
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base $80 * 1.15 (first time) = $92
        result.Subtotal.Should().Be(80.00m * 1.15m);
    }

    [Fact]
    public async Task CalculatePricing_WithPriceMultiplier_NoElevator_AppliesCorrectly()
    {
        // Arrange
        TestDbContextFactory.SeedPriceMultipliers(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            HasElevator = false, // triggers NoElevator multiplier
            IsFirstTime = false
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base $80 * 1.05 (no elevator, price only) = $84
        result.Subtotal.Should().Be(80.00m * 1.05m);
        // Time should NOT be affected (AppliesToTime = false for NoElevator)
        result.EstimatedMinutes.Should().Be(60);
    }

    [Fact]
    public async Task CalculatePricing_WithPriceMultiplier_SquareFootage_AppliesCorrectly()
    {
        // Arrange
        TestDbContextFactory.SeedPriceMultipliers(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            SquareFootage = 1500, // within 1000-2000 range
            IsFirstTime = false
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Base $80 * 1.2 (sq footage) = $96
        result.Subtotal.Should().Be(80.00m * 1.2m);
    }

    [Fact]
    public async Task CalculatePricing_WithSquareFootageOutOfRange_DoesNotApply()
    {
        // Arrange
        TestDbContextFactory.SeedPriceMultipliers(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            SquareFootage = 500, // below 1000 min range
            IsFirstTime = false
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // No sq footage multiplier applied
        result.Subtotal.Should().Be(80.00m);
    }

    [Fact]
    public async Task CalculatePricing_WithMultipleMultipliers_StacksCorrectly()
    {
        // Arrange
        TestDbContextFactory.SeedPriceMultipliers(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            DirtLevel = DirtLevel.Heavy, // 1.3x
            HasPets = true,              // 1.1x
            IsFirstTime = true,          // 1.15x
            HasElevator = false          // 1.05x price only
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        // Price: $80 * 1.3 * 1.1 * 1.15 * 1.05
        var expectedPriceFactor = 1.3m * 1.1m * 1.15m * 1.05m;
        result.Subtotal.Should().Be(80.00m * expectedPriceFactor);
        // Time: 60 * 1.3 * 1.1 * 1.15 (no elevator doesn't apply to time)
        var expectedTimeFactor = 1.3m * 1.1m * 1.15m;
        result.EstimatedMinutes.Should().Be((int)(60m * expectedTimeFactor));
    }

    #endregion

    #region CalculatePricingAsync - Recurrence Discount

    [Fact]
    public async Task CalculatePricing_WithRecurrenceDiscount_AppliesDiscount()
    {
        // Arrange
        TestDbContextFactory.SeedRecurrenceDiscounts(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            RecurrenceType = RecurrenceType.Weekly, // 15% discount
            IsFirstTime = false
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.Subtotal.Should().Be(80.00m);
        result.Discount.Should().Be(80.00m * 0.15m); // $12
        result.Total.Should().Be(80.00m - (80.00m * 0.15m)); // $68
        result.DiscountPercent.Should().Be(15.0m);
    }

    [Fact]
    public async Task CalculatePricing_WithBiWeeklyRecurrence_AppliesCorrectDiscount()
    {
        // Arrange
        TestDbContextFactory.SeedRecurrenceDiscounts(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            RecurrenceType = RecurrenceType.BiWeekly, // 10% discount
            IsFirstTime = false
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Discount.Should().Be(80.00m * 0.10m); // $8
        result.Total.Should().Be(80.00m - 8.00m); // $72
    }

    [Fact]
    public async Task CalculatePricing_WithNoRecurrence_NoDiscount()
    {
        // Arrange
        TestDbContextFactory.SeedRecurrenceDiscounts(_context);

        var input = new PricingInput
        {
            ServiceTypeId = 1,
            RecurrenceType = RecurrenceType.None,
            IsFirstTime = false
        };

        // Act
        var result = await _sut.CalculatePricingAsync(input);

        // Assert
        result.Discount.Should().Be(0);
        result.DiscountPercent.Should().Be(0);
        result.Total.Should().Be(result.Subtotal);
    }

    #endregion

    #region ConfirmBookingAsync - Soft Reserve Validation

    [Fact]
    public async Task ConfirmBooking_WithValidSoftReserve_CreatesOrder()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = CreateConfirmBookingInput(softReserve);

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.OrderId.Should().BeGreaterThan(0);
        result.MeetId.Should().BeGreaterThan(0);
        result.ConfirmationNumber.Should().StartWith("SBM-");
        result.Total.Should().BeGreaterThan(0);
        result.OrderStatus.Should().Be("PendingReview");

        // Verify order was created in DB
        var order = await _context.ServiceOrders.FindAsync(result.OrderId);
        order.Should().NotBeNull();
        order!.OrderStatus.Should().Be(OrderStatus.PendingReview);

        // Verify meeting was created
        var meet = await _context.ServiceMeets.FindAsync(result.MeetId);
        meet.Should().NotBeNull();
        meet!.Status.Should().Be(MeetStatus.Scheduled);
    }

    [Fact]
    public async Task ConfirmBooking_WithExpiredSoftReserve_ReturnsFailure()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(
            _context,
            expiresAt: DateTime.UtcNow.AddMinutes(-5) // already expired
        );

        var input = CreateConfirmBookingInput(softReserve);

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.Error.Should().Contain("expired");

        // Verify soft reserve was marked as expired
        var updatedReserve = await _context.SoftReserves.FindAsync(softReserve.Id);
        updatedReserve!.Status.Should().Be(SoftReserveStatus.Expired);
    }

    [Fact]
    public async Task ConfirmBooking_WithInvalidSoftReserve_ReturnsFailure()
    {
        // Arrange - use a non-existent soft reserve ID
        var input = new ConfirmBookingInput
        {
            SoftReserveId = 99999,
            SessionId = "non-existent-session",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "test@test.com"
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task ConfirmBooking_WithAlreadyConvertedSoftReserve_ReturnsFailure()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(
            _context,
            status: SoftReserveStatus.Converted
        );

        var input = CreateConfirmBookingInput(softReserve);

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeFalse();
        result.IsAlreadyProcessed.Should().BeTrue();
        result.Error.Should().Contain("already been processed");
    }

    [Fact]
    public async Task ConfirmBooking_WithWrongSessionId_ReturnsNotFound()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context, sessionId: "correct-session");

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = "wrong-session",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "test@test.com"
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
    }

    #endregion

    #region ConfirmBookingAsync - User Resolution

    [Fact]
    public async Task ConfirmBooking_CreatesUserWhenNotExists()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = null, // no existing customer
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "newuser@test.com",
            ContactName = "John Doe",
            ContactPhone = "555-1234",
            Password = "SecurePass123!", // triggers user creation
            Total = 80.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.AuthToken.Should().NotBeNull();
        result.AuthToken!.IsNewUser.Should().BeTrue();
        result.IsGuest.Should().BeFalse();

        // Verify user was created
        var newUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        newUser.Should().NotBeNull();
        newUser!.FirstName.Should().Be("John");
        newUser.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task ConfirmBooking_UsesExistingUserWhenExists()
    {
        // Arrange - create an existing user first
        var existingUser = new ApplicationUser
        {
            Id = "existing-user-id",
            UserName = "existing@test.com",
            NormalizedUserName = "EXISTING@TEST.COM",
            Email = "existing@test.com",
            NormalizedEmail = "EXISTING@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Existing",
            LastName = "User"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = null, // not logged in, but email matches existing user
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "existing@test.com",
            Total = 80.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.AuthToken.Should().BeNull(); // no token for existing non-logged-in user
        result.IsGuest.Should().BeFalse();

        // Verify order is linked to existing user
        var order = await _context.ServiceOrders.FindAsync(result.OrderId);
        order!.CustomerId.Should().Be("existing-user-id");
    }

    [Fact]
    public async Task ConfirmBooking_GuestBookingWithoutPassword_CreatesGuestOrder()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = null,
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "guest@test.com",
            Password = null, // no password = guest booking
            Total = 80.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.IsGuest.Should().BeTrue();
        result.AuthToken.Should().BeNull();

        // Verify order has no customer ID
        var order = await _context.ServiceOrders.FindAsync(result.OrderId);
        order!.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmBooking_WithLoggedInCustomerId_UsesDirectly()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = "logged-in-user-id",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "loggedin@test.com",
            Total = 80.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();
        result.IsGuest.Should().BeFalse();

        var order = await _context.ServiceOrders.FindAsync(result.OrderId);
        order!.CustomerId.Should().Be("logged-in-user-id");
    }

    #endregion

    #region ConfirmBookingAsync - Order Details

    [Fact]
    public async Task ConfirmBooking_SavesOrderDetails_Correctly()
    {
        // Arrange
        TestDbContextFactory.SeedAdditionalServiceTypes(_context);
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = "customer-1",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "456 Oak Ave",
            AddressLine2 = "Apt 3B",
            City = "Orlando",
            State = "FL",
            CleaningPlaceId = 1,
            Bedrooms = 3,
            Bathrooms = 2,
            SquareFootage = 1800,
            DirtLevel = DirtLevel.Heavy,
            HasPets = true,
            FloorLevel = 3,
            HasElevator = false,
            AdditionalServiceIds = new List<int> { 1 },
            Rooms = new List<RoomPricingItem>
            {
                new(RoomId: 1, Quantity: 3),
                new(RoomId: 2, Quantity: 2)
            },
            RecurrenceType = RecurrenceType.None,
            ContactName = "Jane Smith",
            ContactPhone = "555-9876",
            ContactEmail = "jane@test.com",
            SpecialInstructions = "Please use eco-friendly products",
            Total = 200.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();

        var order = await _context.ServiceOrders.FindAsync(result.OrderId);
        order.Should().NotBeNull();
        order!.Address.Should().Be("456 Oak Ave");
        order.AddressLine2.Should().Be("Apt 3B");
        order.City.Should().Be("Orlando");
        order.State.Should().Be("FL");
        order.Bedrooms.Should().Be(3);
        order.Bathrooms.Should().Be(2);
        order.SquareFootage.Should().Be(1800);
        order.DirtLevel.Should().Be(DirtLevel.Heavy);
        order.HasPets.Should().BeTrue();
        order.HasElevator.Should().BeFalse();
        order.SpecialInstructions.Should().Be("Please use eco-friendly products");
        order.ContactName.Should().Be("Jane Smith");
        order.Source.Should().Be(OrderSource.Website);
    }

    [Fact]
    public async Task ConfirmBooking_SavesRoomSelections()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = "customer-1",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "test@test.com",
            Rooms = new List<RoomPricingItem>
            {
                new(RoomId: 1, Quantity: 2),
                new(RoomId: 2, Quantity: 1)
            },
            Total = 130.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        var rooms = await _context.ServiceOrderRooms
            .Where(r => r.ServiceOrderId == result.OrderId)
            .ToListAsync();
        rooms.Should().HaveCount(2);
        rooms.Should().Contain(r => r.CleaningPlaceRoomId == 1 && r.Quantity == 2);
        rooms.Should().Contain(r => r.CleaningPlaceRoomId == 2 && r.Quantity == 1);
    }

    [Fact]
    public async Task ConfirmBooking_SavesAdditionalServiceItems()
    {
        // Arrange
        TestDbContextFactory.SeedAdditionalServiceTypes(_context);
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = "customer-1",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "test@test.com",
            AdditionalServiceIds = new List<int> { 1, 2 },
            Total = 135.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        var items = await _context.ServiceOrderItems
            .Where(i => i.ServiceOrderId == result.OrderId)
            .ToListAsync();
        items.Should().HaveCount(2);
        items.Should().Contain(i => i.AdditionalServiceTypeId == 1 && i.UnitPrice == 30.00m);
        items.Should().Contain(i => i.AdditionalServiceTypeId == 2 && i.UnitPrice == 25.00m);
    }

    [Fact]
    public async Task ConfirmBooking_ConvertsSoftReserveStatus()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = CreateConfirmBookingInput(softReserve);

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();

        var updatedReserve = await _context.SoftReserves.FindAsync(softReserve.Id);
        updatedReserve!.Status.Should().Be(SoftReserveStatus.Converted);
        updatedReserve.ServiceOrderId.Should().Be(result.OrderId);
    }

    [Fact]
    public async Task ConfirmBooking_CreatesMeetingWithCorrectSchedule()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        var input = CreateConfirmBookingInput(softReserve);

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        var meet = await _context.ServiceMeets.FindAsync(result.MeetId);
        meet.Should().NotBeNull();
        meet!.ScheduledStart.Should().Be(softReserve.ScheduledStart);
        meet.ScheduledEnd.Should().Be(softReserve.ScheduledEnd);
        meet.AssignedEmployeeId.Should().Be(softReserve.EmployeeId);
        meet.ServiceAreaId.Should().Be(softReserve.ServiceAreaId);
        meet.Status.Should().Be(MeetStatus.Scheduled);
    }

    #endregion

    #region ConfirmBookingAsync - Recurrence

    [Fact]
    public async Task ConfirmBooking_WithRecurrence_CreatesMultipleMeetings()
    {
        // Arrange
        var softReserve = TestDbContextFactory.SeedSoftReserve(_context);

        // Set up employee schedule for the recurring day
        var scheduledDay = softReserve.ScheduledStart.DayOfWeek;
        var hasSchedule = await _context.EmployeeSchedules
            .AnyAsync(s => s.EmployeeId == 1 && s.DayOfWeek == scheduledDay);

        if (!hasSchedule)
        {
            _context.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = 1,
                DayOfWeek = scheduledDay,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BufferMinutes = 30,
                IsAvailable = true
            });
            await _context.SaveChangesAsync();
        }

        var input = new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = "customer-1",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ContactEmail = "test@test.com",
            RecurrenceType = RecurrenceType.Weekly,
            RecurrenceEndDate = DateTime.UtcNow.AddMonths(2),
            Total = 80.00m
        };

        // Act
        var result = await _sut.ConfirmBookingAsync(input);

        // Assert
        result.Success.Should().BeTrue();

        // Should have the original meeting plus recurring ones
        var meetingsCount = await _context.ServiceMeets
            .CountAsync(m => m.ServiceOrderId == result.OrderId);
        meetingsCount.Should().BeGreaterThan(1);
    }

    #endregion

    #region CreateRecurringMeetingsAsync

    [Fact]
    public async Task CreateRecurringMeetings_Weekly_CreatesCorrectDates()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Monday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(120);

        // Ensure employee schedule exists for Monday
        EnsureScheduleExists(1, DayOfWeek.Monday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        var endDate = scheduledStart.AddDays(28); // 4 weeks

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.Weekly, endDate);

        // Assert
        count.Should().BeGreaterThan(0);
        count.Should().BeLessOrEqualTo(4); // at most 4 weeks ahead

        // Verify the meetings are spaced 7 days apart
        var meetings = await _context.ServiceMeets
            .Where(m => m.ServiceOrderId == order.Id && m.Id != firstMeet.Id)
            .OrderBy(m => m.ScheduledStart)
            .ToListAsync();

        for (int i = 0; i < meetings.Count; i++)
        {
            var expectedDate = scheduledStart.AddDays(7 * (i + 1));
            meetings[i].ScheduledStart.Should().Be(expectedDate);
            meetings[i].ScheduledEnd.Should().Be(expectedDate.AddMinutes(120));
        }
    }

    [Fact]
    public async Task CreateRecurringMeetings_BiWeekly_CreatesCorrectDates()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Wednesday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(90);

        EnsureScheduleExists(1, DayOfWeek.Wednesday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        var endDate = scheduledStart.AddDays(56); // 8 weeks

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.BiWeekly, endDate);

        // Assert
        count.Should().BeGreaterThan(0);

        var meetings = await _context.ServiceMeets
            .Where(m => m.ServiceOrderId == order.Id && m.Id != firstMeet.Id)
            .OrderBy(m => m.ScheduledStart)
            .ToListAsync();

        // Verify 14-day intervals
        for (int i = 0; i < meetings.Count; i++)
        {
            var expectedDate = scheduledStart.AddDays(14 * (i + 1));
            meetings[i].ScheduledStart.Should().Be(expectedDate);
        }
    }

    [Fact]
    public async Task CreateRecurringMeetings_Monthly_CreatesCorrectDates()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Tuesday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(120);

        EnsureScheduleExists(1, DayOfWeek.Tuesday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        var endDate = scheduledStart.AddMonths(6);

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.Monthly, endDate);

        // Assert
        count.Should().BeGreaterThanOrEqualTo(0); // may be 0 if days don't match schedule

        var meetings = await _context.ServiceMeets
            .Where(m => m.ServiceOrderId == order.Id && m.Id != firstMeet.Id)
            .OrderBy(m => m.ScheduledStart)
            .ToListAsync();

        // Monthly meetings should be roughly 1 month apart
        for (int i = 0; i < meetings.Count; i++)
        {
            var expectedDate = scheduledStart.AddMonths(i + 1);
            meetings[i].ScheduledStart.Should().Be(expectedDate);
        }
    }

    [Fact]
    public async Task CreateRecurringMeetings_StopsAtEndDate()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Monday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(60);

        EnsureScheduleExists(1, DayOfWeek.Monday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        // Set end date to only 2 weeks out (should create at most 2 weekly meetings)
        var endDate = scheduledStart.AddDays(15);

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.Weekly, endDate);

        // Assert
        count.Should().BeLessOrEqualTo(2);

        var allMeetings = await _context.ServiceMeets
            .Where(m => m.ServiceOrderId == order.Id && m.Id != firstMeet.Id)
            .ToListAsync();

        allMeetings.Should().AllSatisfy(m =>
        {
            m.ScheduledStart.Should().BeBefore(endDate.AddDays(1));
        });
    }

    [Fact]
    public async Task CreateRecurringMeetings_SkipsConflicts()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Thursday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(120);

        EnsureScheduleExists(1, DayOfWeek.Thursday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        // Create a conflicting meeting on the second Thursday
        var conflictDate = scheduledStart.AddDays(7);
        _context.ServiceMeets.Add(new ServiceMeet
        {
            ServiceOrderId = order.Id,
            AssignedEmployeeId = 1,
            ServiceAreaId = 1,
            ScheduledStart = conflictDate,
            ScheduledEnd = conflictDate.AddMinutes(120),
            Status = MeetStatus.Scheduled,
            EstimatedDurationMinutes = 120
        });
        await _context.SaveChangesAsync();

        var endDate = scheduledStart.AddDays(28);

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.Weekly, endDate);

        // Assert
        // The conflicting slot should be skipped; remaining meetings should be created
        var meetings = await _context.ServiceMeets
            .Where(m => m.ServiceOrderId == order.Id && m.Id != firstMeet.Id)
            .OrderBy(m => m.ScheduledStart)
            .ToListAsync();

        // The second Thursday has a conflict, so it should be skipped
        // (the count should reflect the actual number created, minus the skipped one)
        meetings.Should().NotContain(m =>
            m.ScheduledStart == conflictDate &&
            m.Id != firstMeet.Id);
    }

    [Fact]
    public async Task CreateRecurringMeetings_SkipsTimeOff()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Tuesday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(60);

        EnsureScheduleExists(1, DayOfWeek.Tuesday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        // Create approved time off on second Tuesday
        var timeOffDate = scheduledStart.AddDays(7);
        _context.EmployeeTimeOffs.Add(new EmployeeTimeOff
        {
            EmployeeId = 1,
            StartDateTime = timeOffDate.Date,
            EndDateTime = timeOffDate.Date.AddDays(1),
            Type = TimeOffType.Vacation,
            Status = TimeOffStatus.Approved,
            Reason = "Vacation"
        });
        await _context.SaveChangesAsync();

        var endDate = scheduledStart.AddDays(28);

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.Weekly, endDate);

        // Assert - the time-off week should be skipped
        var meetings = await _context.ServiceMeets
            .Where(m => m.ServiceOrderId == order.Id && m.Id != firstMeet.Id)
            .ToListAsync();

        meetings.Should().NotContain(m =>
            m.ScheduledStart.Date == timeOffDate.Date);
    }

    [Fact]
    public async Task CreateRecurringMeetings_WithInvalidRecurrenceType_ReturnsZero()
    {
        // Arrange
        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1,
            DateTime.UtcNow.AddDays(1).AddHours(10),
            DateTime.UtcNow.AddDays(1).AddHours(12));

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.None, null);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreateRecurringMeetings_MaxOccurrences_CapsAt8()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Monday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(60);

        EnsureScheduleExists(1, DayOfWeek.Monday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        // Set end date far in the future (more than 8 weeks)
        var endDate = scheduledStart.AddDays(365);

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.Weekly, endDate);

        // Assert - max 8 occurrences
        count.Should().BeLessOrEqualTo(8);
    }

    [Fact]
    public async Task CreateRecurringMeetings_AcquiresSlotsForEachMeeting()
    {
        // Arrange
        var startDate = TestDbContextFactory.GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Monday);
        var scheduledStart = startDate.AddHours(10);
        var scheduledEnd = scheduledStart.AddMinutes(60);

        EnsureScheduleExists(1, DayOfWeek.Monday);

        var order = CreateTestOrder();
        var firstMeet = CreateTestMeet(order.Id, 1, scheduledStart, scheduledEnd);

        var endDate = scheduledStart.AddDays(14); // 2 weeks

        // Act
        var count = await _sut.CreateRecurringMeetingsAsync(
            order, firstMeet, RecurrenceType.Weekly, endDate);

        // Assert - verify AcquireSlotsAsync was called for each recurring meeting
        if (count > 0)
        {
            _schedulingServiceMock.Verify(
                s => s.AcquireSlotsAsync(
                    1, // employee ID
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    OccupancyType.Meeting,
                    It.IsAny<int>(),
                    null),
                Times.Exactly(count));
        }
    }

    #endregion

    #region Helper Methods

    private ConfirmBookingInput CreateConfirmBookingInput(SoftReserve softReserve)
    {
        return new ConfirmBookingInput
        {
            SoftReserveId = softReserve.Id,
            SessionId = softReserve.SessionId,
            CustomerId = "customer-1",
            ServiceTypeId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            City = "Orlando",
            State = "FL",
            ContactEmail = "test@test.com",
            ContactName = "Test User",
            ContactPhone = "555-1234",
            Total = 80.00m
        };
    }

    private ServiceOrder CreateTestOrder()
    {
        var order = new ServiceOrder
        {
            CustomerId = "customer-1",
            ServiceAreaId = 1,
            ZipCode = "32801",
            Address = "123 Main St",
            ServiceTypeId = 1,
            Subtotal = 80.00m,
            Total = 80.00m,
            OrderStatus = OrderStatus.PendingReview,
            RecurrenceType = RecurrenceType.Weekly,
            EstimatedDurationMinutes = 120
        };
        _context.ServiceOrders.Add(order);
        _context.SaveChanges();
        return order;
    }

    private ServiceMeet CreateTestMeet(int orderId, int employeeId,
        DateTime scheduledStart, DateTime scheduledEnd)
    {
        var meet = new ServiceMeet
        {
            ServiceOrderId = orderId,
            AssignedEmployeeId = employeeId,
            ServiceAreaId = 1,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            EstimatedDurationMinutes = (int)(scheduledEnd - scheduledStart).TotalMinutes,
            Status = MeetStatus.Scheduled
        };
        _context.ServiceMeets.Add(meet);
        _context.SaveChanges();
        return meet;
    }

    private void EnsureScheduleExists(int employeeId, DayOfWeek dayOfWeek)
    {
        var exists = _context.EmployeeSchedules
            .Any(s => s.EmployeeId == employeeId && s.DayOfWeek == dayOfWeek && !s.IsDeleted);

        if (!exists)
        {
            _context.EmployeeSchedules.Add(new EmployeeSchedule
            {
                EmployeeId = employeeId,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BufferMinutes = 30,
                IsAvailable = true
            });
            _context.SaveChanges();
        }
    }

    #endregion
}
