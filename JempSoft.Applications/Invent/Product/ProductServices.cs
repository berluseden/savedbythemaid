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
    public class ProductServices : IProductServices
    {
        private readonly JempSoftDbContext _context;
        private readonly ILogger<ProductServices> _logger;

        public ProductServices(JempSoftDbContext context, ILogger<ProductServices> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ProductOutputDto>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _context.Product
                    .Where(p => p.productId == id)
                    .Select(p => new ProductOutputDto
                    {
                        ProductId = p.productId,
                        ProductCode = p.productCode,
                        ProductName = p.productName,
                        Description = p.description ?? string.Empty,
                        Barcode = p.barcode ?? string.Empty,
                        SerialNumber = p.serialNumber ?? string.Empty,
                        ProductType = p.productType,
                        Uom = p.uom,
                        CreatedAt = p.createdAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product not found: {Id}", id);
                    return Result.Failure<ProductOutputDto>($"Product with ID {id} not found");
                }

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {Id}", id);
                return Result.Failure<ProductOutputDto>($"Error getting product: {ex.Message}");
            }
        }

        public async Task<Result<List<ProductOutputDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Product
                    .OrderByDescending(p => p.createdAt)
                    .Select(p => new ProductOutputDto
                    {
                        ProductId = p.productId,
                        ProductCode = p.productCode,
                        ProductName = p.productName,
                        Description = p.description ?? string.Empty,
                        Barcode = p.barcode ?? string.Empty,
                        SerialNumber = p.serialNumber ?? string.Empty,
                        ProductType = p.productType,
                        Uom = p.uom,
                        CreatedAt = p.createdAt
                    })
                    .ToListAsync(cancellationToken);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                return Result.Failure<List<ProductOutputDto>>($"Error getting products: {ex.Message}");
            }
        }

        public async Task<Result<string>> SaveAsync(ProductInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = new Product
                {
                    productId = Guid.NewGuid().ToString(),
                    productCode = input.ProductCode,
                    productName = input.ProductName,
                    description = input.Description,
                    barcode = input.Barcode,
                    serialNumber = input.SerialNumber,
                    productType = input.ProductType,
                    uom = input.Uom,
                    createdAt = DateTime.UtcNow
                };

                _context.Product.Add(product);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created product {Id}", product.productId);
                return product.productId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving product");
                return Result.Failure<string>($"Error saving product: {ex.Message}");
            }
        }

        public async Task<Result> UpdateByIdAsync(string productId, ProductInputDto input, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _context.Product
                    .FirstOrDefaultAsync(p => p.productId == productId, cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product not found for update: {Id}", productId);
                    return Result.Failure($"Product with ID {productId} not found");
                }

                product.productCode = input.ProductCode;
                product.productName = input.ProductName;
                product.description = input.Description;
                product.barcode = input.Barcode;
                product.serialNumber = input.SerialNumber;
                product.productType = input.ProductType;
                product.uom = input.Uom;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated product {Id}", productId);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!Exists(productId))
                {
                    _logger.LogWarning("Product not found during concurrent update: {Id}", productId);
                    return Result.Failure($"Product with ID {productId} not found");
                }
                _logger.LogError(ex, "Concurrency error updating product {Id}", productId);
                return Result.Failure($"Concurrency error updating product: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {Id}", productId);
                return Result.Failure($"Error updating product: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _context.Product
                    .FirstOrDefaultAsync(p => p.productId == id, cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product not found for deletion: {Id}", id);
                    return Result.Failure($"Product with ID {id} not found");
                }

                _context.Product.Remove(product);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted product {Id}", id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {Id}", id);
                return Result.Failure($"Error deleting product: {ex.Message}");
            }
        }

        public bool Exists(string id)
        {
            return _context.Product.Any(p => p.productId == id);
        }

        public async Task<Result<List<ComboBoxOutPutDto>>> GetComboBoxAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _context.Product
                    .Select(p => new ComboBoxOutPutDto
                    {
                        Id = 0,
                        Name = p.productName,
                        StringId = p.productId
                    })
                    .ToListAsync(cancellationToken);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for combo box");
                return Result.Failure<List<ComboBoxOutPutDto>>($"Error getting products: {ex.Message}");
            }
        }
    }
}
