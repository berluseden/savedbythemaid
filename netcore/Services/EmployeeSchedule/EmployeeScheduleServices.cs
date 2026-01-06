using netcore.Models;
using netcore.Data;
using netcore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace netcore.Services.Services
{
    public class EmployeeScheduleServices : IEmployeeScheduleServices
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeScheduleServices> _logger;

        public EmployeeScheduleServices(ApplicationDbContext context, ILogger<EmployeeScheduleServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<EmployeeSchedule>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.EmployeeSchedules
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.EmployeeScheduleId == id, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning("EmployeeSchedule not found: {Id}", id);
                    return Result.Failure<EmployeeSchedule>($"EmployeeSchedule with ID {id} not found");
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting EmployeeSchedule {Id}", id);
                return Result.Failure<EmployeeSchedule>($"Error getting EmployeeSchedule: {ex.Message}");
            }
        }

        public async Task<Result<List<EmployeeSchedule>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.EmployeeSchedules
                    .Include(x => x.Employee)
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.AvaliableDay)
                    .ToListAsync(cancellationToken);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all EmployeeSchedules");
                return Result.Failure<List<EmployeeSchedule>>($"Error getting EmployeeSchedules: {ex.Message}");
            }
        }

        public async Task<Result<List<EmployeeSchedule>>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.EmployeeSchedules
                    .Where(x => x.EmployeeId == employeeId)
                    .Include(x => x.Employee)
                    .ToListAsync(cancellationToken);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting EmployeeSchedules for Employee {EmployeeId}", employeeId);
                return Result.Failure<List<EmployeeSchedule>>($"Error getting EmployeeSchedules: {ex.Message}");
            }
        }

        public async Task<Result<int>> SaveAsync(EmployeeScheduleInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new EmployeeSchedule for Employee: {EmployeeId}", input.EmployeeId);

                var entity = new EmployeeSchedule
                {
                    EmployeeId = input.EmployeeId,
                    AvaliableDay = input.AvaliableDay,
                    IsActive = input.IsActive
                };

                await _context.EmployeeSchedules.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("EmployeeSchedule created with ID: {Id}", entity.EmployeeScheduleId);
                return entity.EmployeeScheduleId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving EmployeeSchedule");
                return Result.Failure<int>($"Error saving EmployeeSchedule: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(int id, EmployeeScheduleInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.EmployeeSchedules
                    .FirstOrDefaultAsync(x => x.EmployeeScheduleId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"EmployeeSchedule with ID {id} not found");
                }

                entity.EmployeeId = input.EmployeeId;
                entity.AvaliableDay = input.AvaliableDay;
                entity.IsActive = input.IsActive;

                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("EmployeeSchedule updated: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating EmployeeSchedule {Id}", id);
                return Result.Failure($"Error updating EmployeeSchedule: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.EmployeeSchedules
                    .FirstOrDefaultAsync(x => x.EmployeeScheduleId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"EmployeeSchedule with ID {id} not found");
                }

                // Hard delete for schedules
                _context.EmployeeSchedules.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("EmployeeSchedule deleted: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EmployeeSchedule {Id}", id);
                return Result.Failure($"Error deleting EmployeeSchedule: {ex.Message}");
            }
        }

        public bool Exists(int id)
        {
            return _context.EmployeeSchedules.Any(x => x.EmployeeScheduleId == id);
        }
    }
}
