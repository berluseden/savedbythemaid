using netcore.Models;
using netcore.Data;
using netcore.Models;
using netcore.POCOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using netcore.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace netcore.Services.Services
{
    /// <summary>
    /// Service for service type operations with async support
    /// </summary>
    public class ServiceTypeServices : IServiceTypeServices
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceTypeServices> _logger;

        public ServiceTypeServices(ApplicationDbContext context, ILogger<ServiceTypeServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Async Methods (Preferred)

        public async Task<Result<ServiceType>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceType = await _context.ServiceTypes
                    .FirstOrDefaultAsync(s => s.ServiceTypeId == id, cancellationToken);

                if (serviceType == null)
                {
                    _logger.LogWarning("Service type not found: {Id}", id);
                    return Result.Failure<ServiceType>($"Service type with ID {id} not found");
                }

                return serviceType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service type {Id}", id);
                return Result.Failure<ServiceType>($"Error getting service type: {ex.Message}");
            }
        }

        public async Task<Result<ServiceTypeOutputDto>> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceType = await _context.ServiceTypes
                    .Where(s => s.ServiceTypeId == id)
                    .Select(s => new ServiceTypeOutputDto
                    {
                        ServiceTypeId = s.ServiceTypeId,
                        Title = s.Title,
                        FullDescription = s.FullDescription,
                        Cost = s.Cost,
                        Price = s.Price,
                        IsActive = s.IsActive,
                        CreatorUserName = ""
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (serviceType == null)
                {
                    _logger.LogWarning("Service type not found: {Id}", id);
                    return Result.Failure<ServiceTypeOutputDto>($"Service type with ID {id} not found");
                }

                return serviceType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service type {Id}", id);
                return Result.Failure<ServiceTypeOutputDto>($"Error getting service type: {ex.Message}");
            }
        }

        public async Task<Result<List<ServiceType>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceTypes = await _context.ServiceTypes
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Title)
                    .ToListAsync(cancellationToken);

                return serviceTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all service types");
                return Result.Failure<List<ServiceType>>($"Error getting service types: {ex.Message}");
            }
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxByCleaningPlaceRoomIdAsync(int cleaningPlaceRoomId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Query using only mapped columns, then format in memory
                var comboBoxItems = await (
                    from cprs in _context.CleaningPlaceRoomServiceTypes
                        .Where(c => c.CleaningPlaceRoomId == cleaningPlaceRoomId)
                    join st in _context.ServiceTypes on cprs.ServiceTypeId equals st.ServiceTypeId
                    where st.IsActive
                    orderby st.Title
                    select new ComboBoxOutPutDto
                    {
                        Id = st.ServiceTypeId.ToString(),
                        Title = st.Title + " - $USD " + st.Price.ToString("N2"),
                        Price = (decimal)st.Price
                    }
                ).ToListAsync(cancellationToken);

                return comboBoxItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service types for cleaning place room {Id}", cleaningPlaceRoomId);
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting service types: {ex.Message}");
            }
        }

        public async Task<Result<int>> SaveAsync(ServiceTypeInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new service type: {Title}", input.Title);

                var serviceType = new ServiceType
                {
                    Title = input.Title,
                    Cost = input.Cost,
                    Price = input.Price,
                    IsActive = input.IsActive,
                    CreatorUserId = input.CreatorUserId,
                    CreationDate = DateTime.UtcNow
                };

                await _context.ServiceTypes.AddAsync(serviceType, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Service type created with ID: {Id}", serviceType.ServiceTypeId);
                return serviceType.ServiceTypeId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving service type");
                return Result.Failure<int>($"Error saving service type: {ex.Message}");
            }
        }

        public async Task<Result> UpdateAsync(ServiceTypeInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                // ServiceTypeInputDto doesn't have ServiceTypeId, so we need to add it or use a different DTO for updates
                // For now, we'll require an ID parameter for update operations
                _logger.LogWarning("UpdateAsync called but ServiceTypeInputDto doesn't contain ServiceTypeId");
                return Result.Failure("Update operation requires ServiceTypeId. Please provide a valid ID.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service type");
                return Result.Failure($"Error updating service type: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a service type by ID
        /// </summary>
        public async Task<Result> UpdateByIdAsync(int serviceTypeId, ServiceTypeInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceType = await _context.ServiceTypes
                    .FirstOrDefaultAsync(s => s.ServiceTypeId == serviceTypeId, cancellationToken);

                if (serviceType == null)
                {
                    return Result.Failure($"Service type with ID {serviceTypeId} not found");
                }

                serviceType.Title = input.Title;
                serviceType.Cost = input.Cost;
                serviceType.Price = input.Price;
                serviceType.IsActive = input.IsActive;
                serviceType.UpdateUserId = input.CreatorUserId; // Reusing CreatorUserId for update user
                serviceType.UpdateDate = DateTime.UtcNow;

                _context.Entry(serviceType).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Service type updated: {Id}", serviceTypeId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service type {Id}", serviceTypeId);
                return Result.Failure($"Error updating service type: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceType = await _context.ServiceTypes
                    .FirstOrDefaultAsync(s => s.ServiceTypeId == id, cancellationToken);

                if (serviceType == null)
                {
                    return Result.Failure($"Service type with ID {id} not found");
                }

                // Hard delete - remove from database
                _context.ServiceTypes.Remove(serviceType);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Service type deleted: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service type {Id}", id);
                return Result.Failure($"Error deleting service type: {ex.Message}");
            }
        }

        public bool Exists(int id)
        {
            return _context.ServiceTypes.Any(s => s.ServiceTypeId == id);
        }

        #endregion
    }
}
