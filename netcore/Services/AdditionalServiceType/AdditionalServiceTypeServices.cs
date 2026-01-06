using netcore.Models;
using netcore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace netcore.Services.Services
{
    public class AdditionalServiceTypeServices : IAdditionalServiceTypeServices
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdditionalServiceTypeServices> _logger;

        public AdditionalServiceTypeServices(ApplicationDbContext context, ILogger<AdditionalServiceTypeServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AdditionalServiceType>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.AdditionalServiceTypes
                    .FirstOrDefaultAsync(x => x.AdditionalServiceTypeId == id, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning("AdditionalServiceType not found: {Id}", id);
                    return Result.Failure<AdditionalServiceType>($"AdditionalServiceType with ID {id} not found");
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AdditionalServiceType {Id}", id);
                return Result.Failure<AdditionalServiceType>($"Error getting AdditionalServiceType: {ex.Message}");
            }
        }

        public async Task<Result<List<AdditionalServiceType>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.AdditionalServiceTypes.ToListAsync(cancellationToken);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all AdditionalServiceTypes");
                return Result.Failure<List<AdditionalServiceType>>($"Error getting AdditionalServiceTypes: {ex.Message}");
            }
        }

        public async Task<Result<int>> SaveAsync(AdditionalServiceTypeInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new AdditionalServiceType: {Title}", input.Title);

                var entity = new AdditionalServiceType
                {
                    Title = input.Title,
                    Cost = input.Cost,
                    Price = input.Price,
                    IsActive = input.IsActive,
                    CreatorUserId = input.CreatorUserId,
                    CreationDate = DateTime.UtcNow
                };

                await _context.AdditionalServiceTypes.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("AdditionalServiceType created with ID: {Id}", entity.AdditionalServiceTypeId);
                return entity.AdditionalServiceTypeId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AdditionalServiceType");
                return Result.Failure<int>($"Error saving AdditionalServiceType: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(int id, AdditionalServiceTypeInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.AdditionalServiceTypes
                    .FirstOrDefaultAsync(x => x.AdditionalServiceTypeId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"AdditionalServiceType with ID {id} not found");
                }

                entity.Title = input.Title;
                entity.Cost = input.Cost;
                entity.Price = input.Price;
                entity.IsActive = input.IsActive;
                entity.UpdateUserId = input.CreatorUserId;
                entity.UpdateDate = DateTime.UtcNow;

                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("AdditionalServiceType updated: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AdditionalServiceType {Id}", id);
                return Result.Failure($"Error updating AdditionalServiceType: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.AdditionalServiceTypes
                    .FirstOrDefaultAsync(x => x.AdditionalServiceTypeId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"AdditionalServiceType with ID {id} not found");
                }

                // Hard delete - remove from database
                _context.AdditionalServiceTypes.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("AdditionalServiceType deleted: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting AdditionalServiceType {Id}", id);
                return Result.Failure($"Error deleting AdditionalServiceType: {ex.Message}");
            }
        }

        public bool Exists(int id)
        {
            return _context.AdditionalServiceTypes.Any(x => x.AdditionalServiceTypeId == id);
        }
    }
}
