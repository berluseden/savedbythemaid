using netcore.Models;
using netcore.Services.Book.Dto;
using netcore.Data;
using netcore.Models;
using netcore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace netcore.Services.Book
{
    /// <summary>
    /// Service for handling booking operations with proper async patterns and transaction support
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            ApplicationDbContext context, 
            IUnitOfWork unitOfWork,
            ILogger<BookingService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Async Methods (Preferred)

        public async Task<Result<CartItemOutPutDto>> BookAsync(BookInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating cart item for CleaningPlaceId: {CleaningPlaceId}, ServiceTypeId: {ServiceTypeId}", 
                    input.CleaningPlaceId, input.ServiceTypeId);

                // Check for existing cart item first
                var existingItem = await GetCartItemAsync(input, cancellationToken);
                if (existingItem != null)
                {
                    _logger.LogInformation("Returning existing cart item {CartItemId}", existingItem.CartItemId);
                    return existingItem;
                }

                // Create new cart item
                var cartItem = new CartItem
                {
                    CleaningPlaceId = input.CleaningPlaceId,
                    CleaningPlaceRoomId = input.CleaningPlaceRoomId,
                    ServiceTypeId = input.ServiceTypeId,
                };

                await _context.CartItems.AddAsync(cartItem, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Fetch related data efficiently in one query
                var result = await BuildCartItemOutputAsync(cartItem.CartItemId, cancellationToken);
                
                _logger.LogInformation("Cart item created successfully with Id: {CartItemId}", cartItem.CartItemId);
                return result ?? Result.Failure<CartItemOutPutDto>("Failed to build cart item output");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart item");
                return Result.Failure<CartItemOutPutDto>($"Error creating cart item: {ex.Message}");
            }
        }

        public async Task<Result<OrderToCheckOutDto>> ProcessToCheckOutAsync(int cartItemId, CancellationToken cancellationToken = default)
        {
            if (cartItemId <= 0)
            {
                return Result.Failure<OrderToCheckOutDto>("Invalid cart item ID");
            }

            try
            {
                _logger.LogInformation("Processing checkout for CartItemId: {CartItemId}", cartItemId);

                // Single query with all needed data using projection
                var checkoutData = await (
                    from ci in _context.CartItems.Where(c => c.CartItemId == cartItemId)
                    join cp in _context.CleaningPlaces on ci.CleaningPlaceId equals cp.CleaningPlaceId
                    join cpr in _context.CleaningPlaceRooms on ci.CleaningPlaceRoomId equals cpr.CleaningPlaceRoomId
                    join st in _context.ServiceTypes on ci.ServiceTypeId equals st.ServiceTypeId
                    select new OrderToCheckOutDto
                    {
                        CartItemId = ci.CartItemId,
                        CleaningPlace_Title = cp.Title,
                        CleaningPlaceRoom_Title = cpr.Title,
                        ServiceType_Title = st.Title,
                        ServiceType_Price = Convert.ToDecimal(st.Price)
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                if (checkoutData == null)
                {
                    _logger.LogWarning("Cart item not found: {CartItemId}", cartItemId);
                    return Result.Failure<OrderToCheckOutDto>($"Cart item {cartItemId} not found");
                }

                // Load additional service types
                var additionalServices = await _context.AdditionalServiceTypes
                    .Select(ast => new AddtionalServiceTypeListOutputDto
                    {
                        AdditionalServiceTypeId = ast.AdditionalServiceTypeId,
                        Title = ast.Title,
                        Price = Convert.ToDecimal(ast.Price)
                    })
                    .ToListAsync(cancellationToken);

                checkoutData.AdditionalServiceTypes = additionalServices;

                return checkoutData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout for CartItemId: {CartItemId}", cartItemId);
                return Result.Failure<OrderToCheckOutDto>($"Error processing checkout: {ex.Message}");
            }
        }

        public async Task<Result<AvaliableMaidOutputDto>> GetAvailableMaidsAsync(DateTime day, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query only the needed data without loading everything to memory
                var availableMaid = await _context.AvaliableMaids
                    .Where(d => d.DayOfAvaliability.Date == day.Date)
                    .Select(d => new AvaliableMaidOutputDto
                    {
                        Day = d.DayOfAvaliability,
                        QtyAvaliable = d.AvaliableCount - d.ServiceCount,
                        Hour = ""
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return availableMaid ?? new AvaliableMaidOutputDto { Day = day, QtyAvaliable = 0, Hour = "" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available maids for date: {Date}", day);
                return Result.Failure<AvaliableMaidOutputDto>($"Error getting available maids: {ex.Message}");
            }
        }

        public async Task<Result<AvaliableMaidMonthOutputDto>> GetAvailableMaidThisMonthAsync(DateTime day, CancellationToken cancellationToken = default)
        {
            try
            {
                var startOfMonth = new DateTime(day.Year, day.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var availableMaids = await _context.AvaliableMaids
                    .Where(d => d.DayOfAvaliability >= startOfMonth && d.DayOfAvaliability <= endOfMonth)
                    .Select(d => new AvaliableMaidOutputDto
                    {
                        Day = d.DayOfAvaliability,
                        QtyAvaliable = d.AvaliableCount - d.ServiceCount,
                        Hour = ""
                    })
                    .ToListAsync(cancellationToken);

                return new AvaliableMaidMonthOutputDto { AvaliableByDay = availableMaids };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available maids for month: {Month}/{Year}", day.Month, day.Year);
                return Result.Failure<AvaliableMaidMonthOutputDto>($"Error getting monthly availability: {ex.Message}");
            }
        }

        public async Task<Result<int>> GetAvailableMaidsByDayAsync(DateTime day, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.AvaliableMaids
                    .Where(d => d.DayOfAvaliability.Date == day.Date)
                    .Select(d => d.AvaliableCount - d.ServiceCount)
                    .FirstOrDefaultAsync(cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available maids count for date: {Date}", day);
                return Result.Failure<int>($"Error getting availability count: {ex.Message}");
            }
        }

        public async Task<Result<long>> AddToCartAsync(
            ServiceOrderInputDto serviceOrder, 
            List<int> additionalServices, 
            ServiceContactInfoInputDto contactInfo, 
            CancellationToken cancellationToken = default)
        {
            if (contactInfo == null || serviceOrder == null)
            {
                return Result.Failure<long>("Contact info and service order are required");
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            try
            {
                _logger.LogInformation("Adding service order to cart for CartItemId: {CartItemId}", serviceOrder.CartItemId);

                // Get cart item and service type in one query
                var cartItemData = await (
                    from ci in _context.CartItems.Where(c => c.CartItemId == serviceOrder.CartItemId)
                    join st in _context.ServiceTypes on ci.ServiceTypeId equals st.ServiceTypeId
                    select new { CartItem = ci, ServiceType = st }
                ).FirstOrDefaultAsync(cancellationToken);

                if (cartItemData == null)
                {
                    return Result.Failure<long>($"Cart item {serviceOrder.CartItemId} not found");
                }

                // Calculate amounts
                var additionalServicesAmount = await CalculateAdditionalServicesAmountAsync(additionalServices, cancellationToken);
                var itemAmount = cartItemData.ServiceType.Price;
                var totalAmount = Convert.ToDecimal(itemAmount + additionalServicesAmount);
                decimal tax = 0;

                // Create contact info
                var serviceContactInfo = new ServiceOrderContactInfo
                {
                    Name = contactInfo.Name,
                    Email = contactInfo.Email,
                    Phone = contactInfo.Phone,
                    Address = contactInfo.Address,
                    AdditionalServiceInfo = contactInfo.AdditionalServiceInfo
                };

                await _context.ServiceContactsInfo.AddAsync(serviceContactInfo, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Create service order
                var service = new ServiceOrder
                {
                    CartItemId = serviceOrder.CartItemId,
                    Day = serviceOrder.Day,
                    Month = serviceOrder.Month,
                    Year = serviceOrder.Year,
                    Hour = serviceOrder.Hour,
                    Minute = serviceOrder.Minute,
                    IsPayed = false,
                    IsActive = true,
                    IsComplete = false,
                    Amount = totalAmount,
                    Tax = tax,
                    TotalAmount = totalAmount + tax,
                    ServiceContactInfoId = serviceContactInfo.Id
                };

                await _context.ServiceOrders.AddAsync(service, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Add additional services (batch insert)
                if (additionalServices?.Any() == true)
                {
                    var additionalServiceEntities = additionalServices.Select(asId => new ServiceOrderAdditionalService
                    {
                        AdditionalServiceId = asId,
                        ServiceOrderId = service.Id
                    }).ToList();

                    await _context.ServiceOrderAdditionalServices.AddRangeAsync(additionalServiceEntities, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                _logger.LogInformation("Service order created successfully with Id: {ServiceOrderId}", service.Id);
                return service.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding service order to cart");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<long>($"Error adding to cart: {ex.Message}");
            }
        }

        public async Task<Result<ServiceItemsOnCartDto>> GetCartServiceByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Result.Failure<ServiceItemsOnCartDto>("Username is required");
            }

            try
            {
                var itemsOnCart = new ServiceItemsOnCartDto();

                // Optimized single query with all joins
                var services = await (
                    from s in _context.ServiceOrders
                        .Where(s => !s.IsComplete && s.IsActive && !s.IsPayed)
                        .Include(s => s.AdditionalServices)
                        .Include(s => s.CartItem)
                        .Include(s => s.ServiceContactInfo)
                    join cp in _context.CleaningPlaces on s.CartItem.CleaningPlaceId equals cp.CleaningPlaceId
                    join cpr in _context.CleaningPlaceRooms on s.CartItem.CleaningPlaceRoomId equals cpr.CleaningPlaceRoomId
                    join st in _context.ServiceTypes on s.CartItem.ServiceTypeId equals st.ServiceTypeId
                    where s.ServiceContactInfo.Email == userName
                    select new
                    {
                        ServiceId = s.Id,
                        s.CartItemId,
                        CleaningPlace = cp.Title,
                        CleaningPlaceRoom = cpr.Title,
                        ServiceType = st.Title,
                        ServiceTypePrice = st.Price,
                        OrderAmount = s.Amount,
                        OrderTax = s.Tax,
                        OrderTotalAmount = s.TotalAmount,
                        AdditionalServiceIds = s.AdditionalServices.Select(a => a.AdditionalServiceId).ToList()
                    }
                ).ToListAsync(cancellationToken);

                // Get all additional service types in one query
                var additionalServiceIds = services.SelectMany(s => s.AdditionalServiceIds).Distinct().ToList();
                var additionalServiceTypes = additionalServiceIds.Any()
                    ? await _context.AdditionalServiceTypes
                        .Where(ast => additionalServiceIds.Contains(ast.AdditionalServiceTypeId))
                        .ToDictionaryAsync(ast => ast.AdditionalServiceTypeId, cancellationToken)
                    : new Dictionary<int, AdditionalServiceType>();

                foreach (var item in services)
                {
                    var serviceOnCart = new ServicesOnCartDto
                    {
                        ServiceId = item.ServiceId,
                        CartItemId = item.CartItemId,
                        CleaningPlace = item.CleaningPlace,
                        CleaningPlaceRoom = item.CleaningPlaceRoom,
                        ServiceType = item.ServiceType,
                        ServiceTypePrice = Convert.ToDecimal(item.ServiceTypePrice),
                        OrderAmount = item.OrderAmount,
                        OrderTax = Convert.ToDouble(item.OrderTax),
                        OrderTotalAmount = item.OrderTotalAmount
                    };

                    itemsOnCart.Footer.SubTotal += serviceOnCart.ServiceTypePrice;
                    itemsOnCart.Services.Add(serviceOnCart);

                    // Add additional services from cached dictionary
                    foreach (var asId in item.AdditionalServiceIds)
                    {
                        if (additionalServiceTypes.TryGetValue(asId, out var ast))
                        {
                            serviceOnCart.AdditionalServicesOnCart.Add(new AdditionalServiceType
                            {
                                AdditionalServiceTypeId = ast.AdditionalServiceTypeId,
                                Title = ast.Title,
                                Price = ast.Price
                            });
                            itemsOnCart.Footer.SubItemsTotal += Convert.ToDecimal(ast.Price);
                        }
                    }
                }

                itemsOnCart.Footer.SubTotal += itemsOnCart.Footer.SubItemsTotal;
                itemsOnCart.Footer.Total = itemsOnCart.Footer.SubTotal + itemsOnCart.Footer.Tax;

                return itemsOnCart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart services for user: {UserName}", userName);
                return Result.Failure<ServiceItemsOnCartDto>($"Error getting cart: {ex.Message}");
            }
        }

        public async Task<Result<ServiceOrder>> RemoveItemFromCartAsync(long serviceOrderId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Removing service order from cart: {ServiceOrderId}", serviceOrderId);

                var serviceOrder = await _context.ServiceOrders
                    .FirstOrDefaultAsync(s => s.Id == serviceOrderId, cancellationToken);

                if (serviceOrder == null)
                {
                    return Result.Failure<ServiceOrder>($"Service order {serviceOrderId} not found");
                }

                // Hard delete - remove from database
                _context.ServiceOrders.Remove(serviceOrder);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Service order {ServiceOrderId} removed from cart", serviceOrderId);
                return serviceOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing service order {ServiceOrderId} from cart", serviceOrderId);
                return Result.Failure<ServiceOrder>($"Error removing from cart: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<CartItemOutPutDto?> GetCartItemAsync(BookInputDto input, CancellationToken cancellationToken)
        {
            return await (
                from ci in _context.CartItems.Where(c => 
                    c.CleaningPlaceId == input.CleaningPlaceId && 
                    c.CleaningPlaceRoomId == input.CleaningPlaceRoomId && 
                    c.ServiceTypeId == input.ServiceTypeId)
                join cp in _context.CleaningPlaces on ci.CleaningPlaceId equals cp.CleaningPlaceId
                join cpr in _context.CleaningPlaceRooms on ci.CleaningPlaceRoomId equals cpr.CleaningPlaceRoomId
                join st in _context.ServiceTypes on ci.ServiceTypeId equals st.ServiceTypeId
                select new CartItemOutPutDto
                {
                    CartItemId = ci.CartItemId,
                    CleaningPlaceId = ci.CleaningPlaceId,
                    CleaningPlaceTitle = cp.Title,
                    CleaningPlaceRoomId = ci.CleaningPlaceRoomId,
                    CleaningPlaceRoomTitle = cpr.Title,
                    ServiceTypeId = ci.ServiceTypeId,
                    ServiceTypeFullDescription = st.FullDescription,
                    Price = st.Price
                }
            ).FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<CartItemOutPutDto?> BuildCartItemOutputAsync(int cartItemId, CancellationToken cancellationToken)
        {
            return await (
                from ci in _context.CartItems.Where(c => c.CartItemId == cartItemId)
                join cp in _context.CleaningPlaces on ci.CleaningPlaceId equals cp.CleaningPlaceId
                join cpr in _context.CleaningPlaceRooms on ci.CleaningPlaceRoomId equals cpr.CleaningPlaceRoomId
                join st in _context.ServiceTypes on ci.ServiceTypeId equals st.ServiceTypeId
                select new CartItemOutPutDto
                {
                    CartItemId = ci.CartItemId,
                    CleaningPlaceId = ci.CleaningPlaceId,
                    CleaningPlaceTitle = cp.Title,
                    CleaningPlaceRoomId = ci.CleaningPlaceRoomId,
                    CleaningPlaceRoomTitle = cpr.Title,
                    ServiceTypeId = ci.ServiceTypeId,
                    ServiceTypeFullDescription = st.FullDescription,
                    Price = st.Price
                }
            ).FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<double> CalculateAdditionalServicesAmountAsync(List<int> additionalServiceIds, CancellationToken cancellationToken)
        {
            if (additionalServiceIds == null || !additionalServiceIds.Any())
                return 0;

            return await _context.AdditionalServiceTypes
                .Where(ast => additionalServiceIds.Contains(ast.AdditionalServiceTypeId))
                .SumAsync(ast => ast.Price, cancellationToken);
        }

        #endregion
    }
}
