using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using netcore.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.Services
{
    public class CleaningPlaceRoomServices : ICleaningPlaceRoomServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<CleaningPlaceRoomServices> _logger;

        public CleaningPlaceRoomServices(JempSoftDbContext context, ILogger<CleaningPlaceRoomServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CleaningPlaceRoom>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.CleaningPlaceRooms
                    .FirstOrDefaultAsync(c => c.CleaningPlaceRoomId == id, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning("CleaningPlaceRoom not found: {Id}", id);
                    return Result.Failure<CleaningPlaceRoom>($"CleaningPlaceRoom with ID {id} not found");
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CleaningPlaceRoom {Id}", id);
                return Result.Failure<CleaningPlaceRoom>($"Error getting CleaningPlaceRoom: {ex.Message}");
            }
        }

        public async Task<Result<List<CleaningPlaceRoom>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.CleaningPlaceRooms
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Title)
                    .ToListAsync(cancellationToken);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all CleaningPlaceRooms");
                return Result.Failure<List<CleaningPlaceRoom>>($"Error getting CleaningPlaceRooms: {ex.Message}");
            }
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxOutputAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var comboBoxItems = await _context.CleaningPlaceRooms
                    .Where(c => c.IsActive == true)
                    .Select(c => new ComboBoxOutPutDto
                    {
                        Id = c.CleaningPlaceRoomId,
                        Title = c.Title
                    })
                    .ToListAsync(cancellationToken);

                return comboBoxItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CleaningPlaceRoom combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting combo box items: {ex.Message}");
            }
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxByCleaningPlaceIdAsync(int cleaningPlaceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var comboBoxItems = await (
                    from cpcpr in _context.CleaningPlaceCleaningPlaceRooms
                        .Where(c => c.CleaningPlaceId == cleaningPlaceId)
                    join cpr in _context.CleaningPlaceRooms on cpcpr.CleaningPlaceRoomId equals cpr.CleaningPlaceRoomId
                    orderby cpr.Title
                    select new ComboBoxOutPutDto
                    {
                        Id = cpr.CleaningPlaceRoomId,
                        Title = cpr.Title
                    }
                ).ToListAsync(cancellationToken);

                return comboBoxItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CleaningPlaceRooms for CleaningPlace {Id}", cleaningPlaceId);
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting combo box items: {ex.Message}");
            }
        }

        public async Task<Result<int>> SaveAsync(CleaningPlaceRoomInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new CleaningPlaceRoom: {Title}", input.Title);

                var entity = new CleaningPlaceRoom
                {
                    Title = input.Title,
                    IsActive = input.IsActive,
                    CreatorUserId = input.CreateUserId,
                    CreationDate = DateTime.UtcNow
                };

                await _context.CleaningPlaceRooms.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("CleaningPlaceRoom created with ID: {Id}", entity.CleaningPlaceRoomId);
                return entity.CleaningPlaceRoomId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving CleaningPlaceRoom");
                return Result.Failure<int>($"Error saving CleaningPlaceRoom: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(int id, CleaningPlaceRoomInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.CleaningPlaceRooms
                    .FirstOrDefaultAsync(c => c.CleaningPlaceRoomId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"CleaningPlaceRoom with ID {id} not found");
                }

                entity.Title = input.Title;
                entity.IsActive = input.IsActive;
                entity.UpdateUserId = input.CreateUserId;
                entity.UpdateDate = DateTime.UtcNow;

                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("CleaningPlaceRoom updated: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CleaningPlaceRoom {Id}", id);
                return Result.Failure($"Error updating CleaningPlaceRoom: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.CleaningPlaceRooms
                    .FirstOrDefaultAsync(c => c.CleaningPlaceRoomId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"CleaningPlaceRoom with ID {id} not found");
                }

                // Hard delete - remove from database
                _context.CleaningPlaceRooms.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("CleaningPlaceRoom deleted: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting CleaningPlaceRoom {Id}", id);
                return Result.Failure($"Error deleting CleaningPlaceRoom: {ex.Message}");
            }
        }

        public bool Exists(int id)
        {
            return _context.CleaningPlaceRooms.Any(c => c.CleaningPlaceRoomId == id);
        }
    }
}
