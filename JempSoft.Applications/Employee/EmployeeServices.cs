using JempSoft.Core.Data;
using JempSoft.Core.Models;
using JempSoft.Core.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.Services
{
    public class EmployeeServices : IEmployeeServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<EmployeeServices> _logger;

        public EmployeeServices(JempSoftDbContext context, ILogger<EmployeeServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Employee>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeId == id, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning("Employee not found: {Id}", id);
                    return Result.Failure<Employee>($"Employee with ID {id} not found");
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Employee {Id}", id);
                return Result.Failure<Employee>($"Error getting Employee: {ex.Message}");
            }
        }

        public async Task<Result<List<Employee>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Employees
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync(cancellationToken);
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all Employees");
                return Result.Failure<List<Employee>>($"Error getting Employees: {ex.Message}");
            }
        }

        public async Task<Result<int>> SaveAsync(EmployeeInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new Employee: {FirstName} {LastName}", input.FirstName, input.LastName);

                var entity = new Employee
                {
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Identification = input.Identification,
                    Address = input.Address,
                    ContactNumber = input.ContactNumber,
                    EmailAddress = input.EmailAddress,
                    UserId = input.UserId,
                    IsActive = input.IsActive,
                    CreationDate = DateTime.UtcNow
                };

                await _context.Employees.AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Employee created with ID: {Id}", entity.EmployeeId);
                return entity.EmployeeId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Employee");
                return Result.Failure<int>($"Error saving Employee: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(int id, EmployeeInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"Employee with ID {id} not found");
                }

                entity.FirstName = input.FirstName;
                entity.LastName = input.LastName;
                entity.Identification = input.Identification;
                entity.Address = input.Address;
                entity.ContactNumber = input.ContactNumber;
                entity.EmailAddress = input.EmailAddress;
                entity.UserId = input.UserId;
                entity.IsActive = input.IsActive;
                entity.UpdateDate = DateTime.UtcNow;

                _context.Entry(entity).State = EntityState.Modified;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Employee updated: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Employee {Id}", id);
                return Result.Failure($"Error updating Employee: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeId == id, cancellationToken);

                if (entity == null)
                {
                    return Result.Failure($"Employee with ID {id} not found");
                }

                // Hard delete - remove from database
                _context.Employees.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Employee deleted: {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Employee {Id}", id);
                return Result.Failure($"Error deleting Employee: {ex.Message}");
            }
        }

        public bool Exists(int id)
        {
            return _context.Employees.Any(x => x.EmployeeId == id);
        }
    }
}
