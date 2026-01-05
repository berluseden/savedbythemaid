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
    public class CustomerServices : ICustomerServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<CustomerServices> _logger;

        public CustomerServices(JempSoftDbContext context, ILogger<CustomerServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CustomerOutputDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _context.Customer
                    .Where(c => c.customerId == id)
                    .Select(c => new CustomerOutputDto
                    {
                        CustomerId = c.customerId,
                        CustomerName = c.customerName,
                        Description = c.description ?? string.Empty,
                        Size = c.size,
                        Street1 = c.street1 ?? string.Empty,
                        Street2 = c.street2 ?? string.Empty,
                        City = c.city ?? string.Empty,
                        Province = c.province ?? string.Empty,
                        Country = c.country ?? string.Empty,
                        CreatedAt = c.createdAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found: {Id}", id);
                    return Result.Failure<CustomerOutputDto>($"Customer with ID {id} not found");
                }

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {Id}", id);
                return Result.Failure<CustomerOutputDto>($"Error getting customer: {ex.Message}");
            }
        }

        public async Task<Result<List<CustomerOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var customers = await _context.Customer
                    .OrderByDescending(c => c.createdAt)
                    .Select(c => new CustomerOutputDto
                    {
                        CustomerId = c.customerId,
                        CustomerName = c.customerName,
                        Description = c.description ?? string.Empty,
                        Size = c.size,
                        Street1 = c.street1 ?? string.Empty,
                        Street2 = c.street2 ?? string.Empty,
                        City = c.city ?? string.Empty,
                        Province = c.province ?? string.Empty,
                        Country = c.country ?? string.Empty,
                        CreatedAt = c.createdAt
                    })
                    .ToListAsync(cancellationToken);

                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                return Result.Failure<List<CustomerOutputDto>>($"Error getting customers: {ex.Message}");
            }
        }

        public async Task<Result<string>> SaveAsync(CustomerInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = new Customer
                {
                    customerId = Guid.NewGuid().ToString(),
                    customerName = input.CustomerName,
                    description = input.Description,
                    size = input.Size,
                    street1 = input.Street1,
                    street2 = input.Street2,
                    city = input.City,
                    province = input.Province,
                    country = input.Country,
                    createdAt = DateTime.UtcNow
                };

                _context.Customer.Add(customer);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created customer {Id}", customer.customerId);
                return customer.customerId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customer");
                return Result.Failure<string>($"Error saving customer: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(string customerId, CustomerInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _context.Customer
                    .FirstOrDefaultAsync(c => c.customerId == customerId, cancellationToken);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for update: {Id}", customerId);
                    return Result.Failure($"Customer with ID {customerId} not found");
                }

                customer.customerName = input.CustomerName;
                customer.description = input.Description;
                customer.size = input.Size;
                customer.street1 = input.Street1;
                customer.street2 = input.Street2;
                customer.city = input.City;
                customer.province = input.Province;
                customer.country = input.Country;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated customer {Id}", customerId);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!Exists(customerId))
                {
                    _logger.LogWarning("Customer not found during concurrent update: {Id}", customerId);
                    return Result.Failure($"Customer with ID {customerId} not found");
                }
                _logger.LogError(ex, "Concurrency error updating customer {Id}", customerId);
                return Result.Failure($"Concurrency error updating customer: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {Id}", customerId);
                return Result.Failure($"Error updating customer: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _context.Customer
                    .FirstOrDefaultAsync(c => c.customerId == id, cancellationToken);

                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for deletion: {Id}", id);
                    return Result.Failure($"Customer with ID {id} not found");
                }

                _context.Customer.Remove(customer);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted customer {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {Id}", id);
                return Result.Failure($"Error deleting customer: {ex.Message}");
            }
        }

        public bool Exists(string id)
        {
            return _context.Customer.Any(c => c.customerId == id);
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _context.Customer
                    .Select(c => new ComboBoxOutPutDto
                    {
                        Id = 0,
                        Name = c.customerName,
                        StringId = c.customerId
                    })
                    .ToListAsync(cancellationToken);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers for combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting customers: {ex.Message}");
            }
        }
    }
}
