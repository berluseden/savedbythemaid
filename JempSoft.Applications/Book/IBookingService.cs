using JempSoft.Applications.Book.Dto;
using JempSoft.Core.Models;
using JempSoft.Core.Result;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications
{
    /// <summary>
    /// Service interface for booking operations
    /// </summary>
    public interface IBookingService
    {
        /// <summary>
        /// Creates a new cart item for booking
        /// </summary>
        Task<Result<CartItemOutPutDto>> BookAsync(BookInputDto input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes the cart item for checkout
        /// </summary>
        Task<Result<OrderToCheckOutDto>> ProcessToCheckOutAsync(int cartItemId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available maids for a specific day
        /// </summary>
        Task<Result<AvaliableMaidOutputDto>> GetAvailableMaidsAsync(DateTime day, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available maids for the entire month
        /// </summary>
        Task<Result<AvaliableMaidMonthOutputDto>> GetAvailableMaidThisMonthAsync(DateTime day, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets count of available maids for a specific day
        /// </summary>
        Task<Result<int>> GetAvailableMaidsByDayAsync(DateTime day, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a service order to cart with all details
        /// </summary>
        Task<Result<long>> AddToCartAsync(ServiceOrderInputDto serviceOrder, List<int> additionalServices, ServiceContactInfoInputDto contactInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all cart items for a user
        /// </summary>
        Task<Result<ServiceItemsOnCartDto>> GetCartServiceByUserNameAsync(string userName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item from cart (soft delete)
        /// </summary>
        Task<Result<ServiceOrder>> RemoveItemFromCartAsync(long serviceOrderId, CancellationToken cancellationToken = default);
    }
}