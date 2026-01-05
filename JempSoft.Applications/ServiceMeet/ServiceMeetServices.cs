using JempSoft.Applications.ServiceMeet.Dto;
using JempSoft.Core.Data;
using JempSoft.Core.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.ServiceMeet
{
    /// <summary>
    /// Service for service meet (appointment) operations with async support
    /// </summary>
    public class ServiceMeetServices : IServiceMeetServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<ServiceMeetServices> _logger;

        public ServiceMeetServices(JempSoftDbContext context, ILogger<ServiceMeetServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ServiceMeetOutputDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceMeet = await _context.ServiceMeeting
                    .Include(s => s.CartItem)
                    .Where(s => s.ServiceMeetId == id)
                    .Select(s => new ServiceMeetOutputDto
                    {
                        ServiceMeetId = s.ServiceMeetId,
                        CartItemId = s.CartItemId,
                        Title = s.Title,
                        Address = s.Address,
                        Day = s.Day,
                        Month = s.Month,
                        Year = s.Year,
                        Hour = s.Hour,
                        Minute = s.Minute,
                        IsMorning = s.isMorning,
                        CartItemDescription = s.CartItem != null ? s.CartItem.CartItemId.ToString() : ""
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (serviceMeet == null)
                {
                    _logger.LogWarning("Service meet not found: {Id}", id);
                    return Result.Failure<ServiceMeetOutputDto>($"Service meet with ID {id} not found");
                }

                return serviceMeet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service meet {Id}", id);
                return Result.Failure<ServiceMeetOutputDto>($"Error getting service meet: {ex.Message}");
            }
        }

        public async Task<Result<List<ServiceMeetOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceMeets = await _context.ServiceMeeting
                    .Include(s => s.CartItem)
                    .Select(s => new ServiceMeetOutputDto
                    {
                        ServiceMeetId = s.ServiceMeetId,
                        CartItemId = s.CartItemId,
                        Title = s.Title,
                        Address = s.Address,
                        Day = s.Day,
                        Month = s.Month,
                        Year = s.Year,
                        Hour = s.Hour,
                        Minute = s.Minute,
                        IsMorning = s.isMorning,
                        CartItemDescription = s.CartItem != null ? s.CartItem.CartItemId.ToString() : ""
                    })
                    .ToListAsync(cancellationToken);

                return serviceMeets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all service meets");
                return Result.Failure<List<ServiceMeetOutputDto>>($"Error getting service meets: {ex.Message}");
            }
        }

        public async Task<Result<List<ServiceMeetOutputDto>>> GetByCartItemIdAsync(int cartItemId, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceMeets = await _context.ServiceMeeting
                    .Include(s => s.CartItem)
                    .Where(s => s.CartItemId == cartItemId)
                    .Select(s => new ServiceMeetOutputDto
                    {
                        ServiceMeetId = s.ServiceMeetId,
                        CartItemId = s.CartItemId,
                        Title = s.Title,
                        Address = s.Address,
                        Day = s.Day,
                        Month = s.Month,
                        Year = s.Year,
                        Hour = s.Hour,
                        Minute = s.Minute,
                        IsMorning = s.isMorning,
                        CartItemDescription = s.CartItem != null ? s.CartItem.CartItemId.ToString() : ""
                    })
                    .ToListAsync(cancellationToken);

                return serviceMeets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service meets by cart item {CartItemId}", cartItemId);
                return Result.Failure<List<ServiceMeetOutputDto>>($"Error getting service meets: {ex.Message}");
            }
        }

        public async Task<Result<int>> SaveAsync(ServiceMeetInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceMeet = new JempSoft.Core.Models.ServiceMeet
                {
                    CartItemId = input.CartItemId,
                    Title = input.Title,
                    Address = input.Address,
                    Day = input.Day,
                    Month = input.Month,
                    Year = input.Year,
                    Hour = input.Hour,
                    Minute = input.Minute,
                    isMorning = input.IsMorning
                };

                _context.ServiceMeeting.Add(serviceMeet);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created service meet {Id}", serviceMeet.ServiceMeetId);
                return serviceMeet.ServiceMeetId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving service meet");
                return Result.Failure<int>($"Error saving service meet: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(int serviceMeetId, ServiceMeetInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceMeet = await _context.ServiceMeeting
                    .FirstOrDefaultAsync(s => s.ServiceMeetId == serviceMeetId, cancellationToken);

                if (serviceMeet == null)
                {
                    _logger.LogWarning("Service meet not found for update: {Id}", serviceMeetId);
                    return Result.Failure($"Service meet with ID {serviceMeetId} not found");
                }

                serviceMeet.CartItemId = input.CartItemId;
                serviceMeet.Title = input.Title;
                serviceMeet.Address = input.Address;
                serviceMeet.Day = input.Day;
                serviceMeet.Month = input.Month;
                serviceMeet.Year = input.Year;
                serviceMeet.Hour = input.Hour;
                serviceMeet.Minute = input.Minute;
                serviceMeet.isMorning = input.IsMorning;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated service meet {Id}", serviceMeetId);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!Exists(serviceMeetId))
                {
                    _logger.LogWarning("Service meet not found during concurrent update: {Id}", serviceMeetId);
                    return Result.Failure($"Service meet with ID {serviceMeetId} not found");
                }
                _logger.LogError(ex, "Concurrency error updating service meet {Id}", serviceMeetId);
                return Result.Failure($"Concurrency error updating service meet: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service meet {Id}", serviceMeetId);
                return Result.Failure($"Error updating service meet: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceMeet = await _context.ServiceMeeting
                    .FirstOrDefaultAsync(s => s.ServiceMeetId == id, cancellationToken);

                if (serviceMeet == null)
                {
                    _logger.LogWarning("Service meet not found for deletion: {Id}", id);
                    return Result.Failure($"Service meet with ID {id} not found");
                }

                _context.ServiceMeeting.Remove(serviceMeet);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted service meet {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service meet {Id}", id);
                return Result.Failure($"Error deleting service meet: {ex.Message}");
            }
        }

        public bool Exists(int id)
        {
            return _context.ServiceMeeting.Any(e => e.ServiceMeetId == id);
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetCartItemsComboBoxAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _context.CartItems
                    .Select(c => new ComboBoxOutPutDto
                    {
                        Id = c.CartItemId,
                        Name = c.CartItemId.ToString()
                    })
                    .ToListAsync(cancellationToken);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart items for combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting cart items: {ex.Message}");
            }
        }
    }
}
