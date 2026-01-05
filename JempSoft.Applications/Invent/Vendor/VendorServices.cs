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
    public class VendorServices : IVendorServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<VendorServices> _logger;

        public VendorServices(JempSoftDbContext context, ILogger<VendorServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<VendorOutputDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var vendor = await _context.Vendor
                    .Where(v => v.vendorId == id)
                    .Select(v => new VendorOutputDto
                    {
                        VendorId = v.vendorId,
                        VendorName = v.vendorName,
                        Description = v.description ?? string.Empty,
                        Size = v.size,
                        Street1 = v.street1 ?? string.Empty,
                        Street2 = v.street2 ?? string.Empty,
                        City = v.city ?? string.Empty,
                        Province = v.province ?? string.Empty,
                        Country = v.country ?? string.Empty,
                        CreatedAt = v.createdAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (vendor == null)
                {
                    _logger.LogWarning("Vendor not found: {Id}", id);
                    return Result.Failure<VendorOutputDto>($"Vendor with ID {id} not found");
                }

                return vendor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendor {Id}", id);
                return Result.Failure<VendorOutputDto>($"Error getting vendor: {ex.Message}");
            }
        }

        public async Task<Result<List<VendorOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var vendors = await _context.Vendor
                    .OrderByDescending(v => v.createdAt)
                    .Select(v => new VendorOutputDto
                    {
                        VendorId = v.vendorId,
                        VendorName = v.vendorName,
                        Description = v.description ?? string.Empty,
                        Size = v.size,
                        Street1 = v.street1 ?? string.Empty,
                        Street2 = v.street2 ?? string.Empty,
                        City = v.city ?? string.Empty,
                        Province = v.province ?? string.Empty,
                        Country = v.country ?? string.Empty,
                        CreatedAt = v.createdAt
                    })
                    .ToListAsync(cancellationToken);

                return vendors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vendors");
                return Result.Failure<List<VendorOutputDto>>($"Error getting vendors: {ex.Message}");
            }
        }

        public async Task<Result<string>> SaveAsync(VendorInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var vendor = new Vendor
                {
                    vendorId = Guid.NewGuid().ToString(),
                    vendorName = input.VendorName,
                    description = input.Description,
                    size = input.Size,
                    street1 = input.Street1,
                    street2 = input.Street2,
                    city = input.City,
                    province = input.Province,
                    country = input.Country,
                    createdAt = DateTime.UtcNow
                };

                _context.Vendor.Add(vendor);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created vendor {Id}", vendor.vendorId);
                return vendor.vendorId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving vendor");
                return Result.Failure<string>($"Error saving vendor: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(string vendorId, VendorInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var vendor = await _context.Vendor
                    .FirstOrDefaultAsync(v => v.vendorId == vendorId, cancellationToken);

                if (vendor == null)
                {
                    _logger.LogWarning("Vendor not found for update: {Id}", vendorId);
                    return Result.Failure($"Vendor with ID {vendorId} not found");
                }

                vendor.vendorName = input.VendorName;
                vendor.description = input.Description;
                vendor.size = input.Size;
                vendor.street1 = input.Street1;
                vendor.street2 = input.Street2;
                vendor.city = input.City;
                vendor.province = input.Province;
                vendor.country = input.Country;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated vendor {Id}", vendorId);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!Exists(vendorId))
                {
                    _logger.LogWarning("Vendor not found during concurrent update: {Id}", vendorId);
                    return Result.Failure($"Vendor with ID {vendorId} not found");
                }
                _logger.LogError(ex, "Concurrency error updating vendor {Id}", vendorId);
                return Result.Failure($"Concurrency error updating vendor: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor {Id}", vendorId);
                return Result.Failure($"Error updating vendor: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var vendor = await _context.Vendor
                    .FirstOrDefaultAsync(v => v.vendorId == id, cancellationToken);

                if (vendor == null)
                {
                    _logger.LogWarning("Vendor not found for deletion: {Id}", id);
                    return Result.Failure($"Vendor with ID {id} not found");
                }

                _context.Vendor.Remove(vendor);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted vendor {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor {Id}", id);
                return Result.Failure($"Error deleting vendor: {ex.Message}");
            }
        }

        public bool Exists(string id)
        {
            return _context.Vendor.Any(v => v.vendorId == id);
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _context.Vendor
                    .Select(v => new ComboBoxOutPutDto
                    {
                        Id = 0,
                        Name = v.vendorName,
                        StringId = v.vendorId
                    })
                    .ToListAsync(cancellationToken);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vendors for combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting vendors: {ex.Message}");
            }
        }
    }
}
