using JempSoft.Applications.Invent.Dto;
using JempSoft.Core.Data;
using JempSoft.Core.Models.Invent;
using JempSoft.Core.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JempSoft.Applications.Invent
{
    public class WarehouseServices : IWarehouseServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<WarehouseServices> _logger;

        public WarehouseServices(JempSoftDbContext context, ILogger<WarehouseServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<WarehouseOutputDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var warehouse = await _context.Warehouse
                    .Include(w => w.branch)
                    .Where(w => w.warehouseId == id)
                    .Select(w => new WarehouseOutputDto
                    {
                        WarehouseId = w.warehouseId,
                        BranchId = w.branchId,
                        BranchName = w.branch != null ? w.branch.branchName : string.Empty,
                        WarehouseName = w.warehouseName,
                        Description = w.description ?? string.Empty,
                        Street1 = w.street1 ?? string.Empty,
                        Street2 = w.street2 ?? string.Empty,
                        City = w.city ?? string.Empty,
                        Province = w.province ?? string.Empty,
                        Country = w.country ?? string.Empty,
                        CreatedAt = w.createdAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse not found: {Id}", id);
                    return Result.Failure<WarehouseOutputDto>($"Warehouse with ID {id} not found");
                }

                return warehouse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse {Id}", id);
                return Result.Failure<WarehouseOutputDto>($"Error getting warehouse: {ex.Message}");
            }
        }

        public async Task<Result<List<WarehouseOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var warehouses = await _context.Warehouse
                    .Include(w => w.branch)
                    .OrderByDescending(w => w.createdAt)
                    .Select(w => new WarehouseOutputDto
                    {
                        WarehouseId = w.warehouseId,
                        BranchId = w.branchId,
                        BranchName = w.branch != null ? w.branch.branchName : string.Empty,
                        WarehouseName = w.warehouseName,
                        Description = w.description ?? string.Empty,
                        Street1 = w.street1 ?? string.Empty,
                        Street2 = w.street2 ?? string.Empty,
                        City = w.city ?? string.Empty,
                        Province = w.province ?? string.Empty,
                        Country = w.country ?? string.Empty,
                        CreatedAt = w.createdAt
                    })
                    .ToListAsync(cancellationToken);

                return warehouses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all warehouses");
                return Result.Failure<List<WarehouseOutputDto>>($"Error getting warehouses: {ex.Message}");
            }
        }

        public async Task<Result<string>> SaveAsync(WarehouseInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var warehouse = new Warehouse
                {
                    warehouseId = Guid.NewGuid().ToString(),
                    branchId = input.BranchId,
                    warehouseName = input.WarehouseName,
                    description = input.Description,
                    street1 = input.Street1,
                    street2 = input.Street2,
                    city = input.City,
                    province = input.Province,
                    country = input.Country,
                    createdAt = DateTime.UtcNow
                };

                _context.Warehouse.Add(warehouse);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created warehouse {Id}", warehouse.warehouseId);
                return warehouse.warehouseId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving warehouse");
                return Result.Failure<string>($"Error saving warehouse: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(string warehouseId, WarehouseInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var warehouse = await _context.Warehouse
                    .FirstOrDefaultAsync(w => w.warehouseId == warehouseId, cancellationToken);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse not found for update: {Id}", warehouseId);
                    return Result.Failure($"Warehouse with ID {warehouseId} not found");
                }

                warehouse.branchId = input.BranchId;
                warehouse.warehouseName = input.WarehouseName;
                warehouse.description = input.Description;
                warehouse.street1 = input.Street1;
                warehouse.street2 = input.Street2;
                warehouse.city = input.City;
                warehouse.province = input.Province;
                warehouse.country = input.Country;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated warehouse {Id}", warehouseId);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!Exists(warehouseId))
                {
                    _logger.LogWarning("Warehouse not found during concurrent update: {Id}", warehouseId);
                    return Result.Failure($"Warehouse with ID {warehouseId} not found");
                }
                _logger.LogError(ex, "Concurrency error updating warehouse {Id}", warehouseId);
                return Result.Failure($"Concurrency error updating warehouse: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating warehouse {Id}", warehouseId);
                return Result.Failure($"Error updating warehouse: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var warehouse = await _context.Warehouse
                    .FirstOrDefaultAsync(w => w.warehouseId == id, cancellationToken);

                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse not found for deletion: {Id}", id);
                    return Result.Failure($"Warehouse with ID {id} not found");
                }

                _context.Warehouse.Remove(warehouse);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted warehouse {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting warehouse {Id}", id);
                return Result.Failure($"Error deleting warehouse: {ex.Message}");
            }
        }

        public bool Exists(string id)
        {
            return _context.Warehouse.Any(w => w.warehouseId == id);
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _context.Warehouse
                    .Select(w => new ComboBoxOutPutDto
                    {
                        Id = 0,
                        Name = w.warehouseName,
                        StringId = w.warehouseId
                    })
                    .ToListAsync(cancellationToken);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouses for combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting warehouses: {ex.Message}");
            }
        }
    }
}
