using JempSoft.Core.Data;
using JempSoft.Core.Exceptions;
using JempSoft.Core.Models;
using JempSoft.Core.POCOs;
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
    /// <summary>
    /// Service for cleaning place operations with async support
    /// </summary>
    public class CleaningPlaceServices : ICleaningPlaceServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<CleaningPlaceServices> _logger;

        public CleaningPlaceServices(JempSoftDbContext context, ILogger<CleaningPlaceServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Async Methods (Preferred)

        public async Task<Result<CleaningPlace>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var cleaningPlace = await _context.CleaningPlaces
                    .FirstOrDefaultAsync(c => c.CleaningPlaceId == id, cancellationToken);

                if (cleaningPlace == null)
                {
                    _logger.LogWarning("Cleaning place not found: {Id}", id);
                    return Result.Failure<CleaningPlace>($"Cleaning place with ID {id} not found");
                }

                return cleaningPlace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cleaning place {Id}", id);
                return Result.Failure<CleaningPlace>($"Error getting cleaning place: {ex.Message}");
            }
        }

        public async Task<Result<CleaningPlace>> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var cleaningPlace = await _context.CleaningPlaces
                    .Where(c => c.CleaningPlaceId == id)
                    .Select(c => new CleaningPlace
                    {
                        CleaningPlaceId = c.CleaningPlaceId,
                        Title = c.Title,
                        IsActive = c.IsActive,
                        CreationDate = c.CreationDate,
                        CreatorUserId = c.CreatorUserId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (cleaningPlace == null)
                {
                    _logger.LogWarning("Cleaning place not found: {Id}", id);
                    return Result.Failure<CleaningPlace>($"Cleaning place with ID {id} not found");
                }

                return cleaningPlace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cleaning place {Id}", id);
                return Result.Failure<CleaningPlace>($"Error getting cleaning place: {ex.Message}");
            }
        }

        public async Task<Result<List<CleaningPlace>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Use Where BEFORE ToListAsync to filter at database level
                var cleaningPlaces = await _context.CleaningPlaces
                    .Where(c => c.IsActive == true)
                    .ToListAsync(cancellationToken);

                return cleaningPlaces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all cleaning places");
                return Result.Failure<List<CleaningPlace>>($"Error getting cleaning places: {ex.Message}");
            }
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxOutputAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Use projection and filter at database level
                var comboBoxItems = await _context.CleaningPlaces
                    .Where(c => c.IsActive == true)
                    .Select(c => new ComboBoxOutPutDto
                    {
                        Id = c.CleaningPlaceId,
                        Title = c.Title
                    })
                    .ToListAsync(cancellationToken);

                return comboBoxItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cleaning place combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting combo box items: {ex.Message}");
            }
        }

        public async Task<Result<int>> SaveAsync(CleaningPlaceInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new cleaning place: {Title}", input.Title);

                var cleaningPlace = new CleaningPlace
                {
                    Title = input.Title,
                    CreationDate = DateTime.UtcNow,
                    IsActive = input.IsActive,
                    CreatorUserId = input.CreateUserId
                };

                await _context.CleaningPlaces.AddAsync(cleaningPlace, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaning place created with ID: {Id}", cleaningPlace.CleaningPlaceId);
                return cleaningPlace.CleaningPlaceId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cleaning place");
                return Result.Failure<int>($"Error saving cleaning place: {ex.Message}");
            }
        }

        public async Task<Result> UpdateAsync(CleaningPlaceInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var cleaningPlace = await _context.CleaningPlaces
                    .FirstOrDefaultAsync(c => c.CleaningPlaceId == input.CleaningPlaceId, cancellationToken);

                if (cleaningPlace == null)
                {
                    return Result.Failure($"Cleaning place with ID {input.CleaningPlaceId} not found");
                }

                cleaningPlace.Title = input.Title;
                cleaningPlace.IsActive = input.IsActive;
                cleaningPlace.UpdateDate = DateTime.UtcNow;
                cleaningPlace.UpdateUserId = input.CreateUserId;
                
                _context.Entry(cleaningPlace).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaning place updated: {Id}", input.CleaningPlaceId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cleaning place {Id}", input.CleaningPlaceId);
                return Result.Failure($"Error updating cleaning place: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(int id, CleaningPlaceInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var cleaningPlace = await _context.CleaningPlaces
                    .FirstOrDefaultAsync(c => c.CleaningPlaceId == id, cancellationToken);

                if (cleaningPlace == null)
                {
                    return Result.Failure($"Cleaning place with ID {id} not found");
                }

                cleaningPlace.Title = input.Title;
                cleaningPlace.IsActive = input.IsActive;
                cleaningPlace.UpdateDate = DateTime.UtcNow;
                cleaningPlace.UpdateUserId = input.CreateUserId;
                
                _context.Entry(cleaningPlace).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaning place updated: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cleaning place {Id}", id);
                return Result.Failure($"Error updating cleaning place: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var cleaningPlace = await _context.CleaningPlaces
                    .FirstOrDefaultAsync(c => c.CleaningPlaceId == id, cancellationToken);

                if (cleaningPlace == null)
                {
                    return Result.Failure($"Cleaning place with ID {id} not found");
                }

                // Soft delete
                cleaningPlace.IsActive = false;
                _context.Entry(cleaningPlace).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaning place soft-deleted: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cleaning place {Id}", id);
                return Result.Failure($"Error deleting cleaning place: {ex.Message}");
            }
        }

        public bool Exists(int id)
        {
            return _context.CleaningPlaces.Any(c => c.CleaningPlaceId == id);
        }

        #endregion
    }
}
