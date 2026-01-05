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
    public class BranchServices : IBranchServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<BranchServices> _logger;

        public BranchServices(JempSoftDbContext context, ILogger<BranchServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<BranchOutputDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var branch = await _context.Branch
                    .Where(b => b.branchId == id)
                    .Select(b => new BranchOutputDto
                    {
                        BranchId = b.branchId,
                        BranchName = b.branchName,
                        Description = b.description ?? string.Empty,
                        Street1 = b.street1 ?? string.Empty,
                        Street2 = b.street2 ?? string.Empty,
                        City = b.city ?? string.Empty,
                        Province = b.province ?? string.Empty,
                        Country = b.country ?? string.Empty,
                        IsDefaultBranch = b.isDefaultBranch,
                        CreatedAt = b.createdAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (branch == null)
                {
                    _logger.LogWarning("Branch not found: {Id}", id);
                    return Result.Failure<BranchOutputDto>($"Branch with ID {id} not found");
                }

                return branch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch {Id}", id);
                return Result.Failure<BranchOutputDto>($"Error getting branch: {ex.Message}");
            }
        }

        public async Task<Result<List<BranchOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var branches = await _context.Branch
                    .OrderByDescending(b => b.createdAt)
                    .Select(b => new BranchOutputDto
                    {
                        BranchId = b.branchId,
                        BranchName = b.branchName,
                        Description = b.description ?? string.Empty,
                        Street1 = b.street1 ?? string.Empty,
                        Street2 = b.street2 ?? string.Empty,
                        City = b.city ?? string.Empty,
                        Province = b.province ?? string.Empty,
                        Country = b.country ?? string.Empty,
                        IsDefaultBranch = b.isDefaultBranch,
                        CreatedAt = b.createdAt
                    })
                    .ToListAsync(cancellationToken);

                return branches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all branches");
                return Result.Failure<List<BranchOutputDto>>($"Error getting branches: {ex.Message}");
            }
        }

        public async Task<Result<string>> SaveAsync(BranchInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var branch = new Branch
                {
                    branchId = Guid.NewGuid().ToString(),
                    branchName = input.BranchName,
                    description = input.Description,
                    street1 = input.Street1,
                    street2 = input.Street2,
                    city = input.City,
                    province = input.Province,
                    country = input.Country,
                    isDefaultBranch = input.IsDefaultBranch,
                    createdAt = DateTime.UtcNow
                };

                _context.Branch.Add(branch);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created branch {Id}", branch.branchId);
                return branch.branchId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving branch");
                return Result.Failure<string>($"Error saving branch: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(string branchId, BranchInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var branch = await _context.Branch
                    .FirstOrDefaultAsync(b => b.branchId == branchId, cancellationToken);

                if (branch == null)
                {
                    _logger.LogWarning("Branch not found for update: {Id}", branchId);
                    return Result.Failure($"Branch with ID {branchId} not found");
                }

                branch.branchName = input.BranchName;
                branch.description = input.Description;
                branch.street1 = input.Street1;
                branch.street2 = input.Street2;
                branch.city = input.City;
                branch.province = input.Province;
                branch.country = input.Country;
                branch.isDefaultBranch = input.IsDefaultBranch;

                await _context.SaveChangesAsync(cancellationToken);

                // If this branch is set as default, unset others
                if (input.IsDefaultBranch)
                {
                    var otherBranches = await _context.Branch
                        .Where(b => b.branchId != branchId && b.isDefaultBranch)
                        .ToListAsync(cancellationToken);

                    foreach (var other in otherBranches)
                    {
                        other.isDefaultBranch = false;
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Updated branch {Id}", branchId);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!Exists(branchId))
                {
                    _logger.LogWarning("Branch not found during concurrent update: {Id}", branchId);
                    return Result.Failure($"Branch with ID {branchId} not found");
                }
                _logger.LogError(ex, "Concurrency error updating branch {Id}", branchId);
                return Result.Failure($"Concurrency error updating branch: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch {Id}", branchId);
                return Result.Failure($"Error updating branch: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var branch = await _context.Branch
                    .FirstOrDefaultAsync(b => b.branchId == id, cancellationToken);

                if (branch == null)
                {
                    _logger.LogWarning("Branch not found for deletion: {Id}", id);
                    return Result.Failure($"Branch with ID {id} not found");
                }

                _context.Branch.Remove(branch);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted branch {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch {Id}", id);
                return Result.Failure($"Error deleting branch: {ex.Message}");
            }
        }

        public bool Exists(string id)
        {
            return _context.Branch.Any(b => b.branchId == id);
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _context.Branch
                    .Select(b => new ComboBoxOutPutDto
                    {
                        Id = 0,
                        Name = b.branchName,
                        StringId = b.branchId
                    })
                    .ToListAsync(cancellationToken);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branches for combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting branches: {ex.Message}");
            }
        }
    }
}
