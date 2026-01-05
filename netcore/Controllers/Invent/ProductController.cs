using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using JempSoft.Applications.Invent;
using JempSoft.Applications.Invent.Dto;

namespace netcore.Controllers.Invent
{
    [Authorize(Roles = "Product")]
    public class ProductController : Controller
    {
        private readonly IProductServices _productServices;

        public ProductController(IProductServices productServices)
        {
            _productServices = productServices;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var result = await _productServices.GetAllAsync();
            return View(result.IsSuccess ? result.Value : new List<ProductOutputDto>());
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _productServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _productServices.SaveAsync(input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", result.Error);
            }
            return View(input);
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _productServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var product = result.Value;
            var input = new ProductInputDto
            {
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Description = product.Description,
                Barcode = product.Barcode,
                SerialNumber = product.SerialNumber,
                ProductType = product.ProductType,
                Uom = product.Uom
            };
            ViewData["ProductId"] = id;
            return View(input);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ProductInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _productServices.UpdateByIdAsync(id, input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Index));
                }

                if (result.Error.Contains("not found"))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", result.Error);
            }
            ViewData["ProductId"] = id;
            return View(input);
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _productServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var result = await _productServices.DeleteAsync(id);
            if (result.IsFailure)
            {
                var productResult = await _productServices.GetByIdAsync(id);
                if (productResult.IsSuccess)
                {
                    ViewData["StatusMessage"] = "Error. Calm Down ^_^ and please contact your SysAdmin with this message: " + result.Error;
                    return View(productResult.Value);
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}





namespace netcore.MVC
{
    public static partial class Pages
    {
        public static class Product
        {
            public const string Controller = "Product";
            public const string Action = "Index";
            public const string Role = "Product";
            public const string Url = "/Product/Index";
            public const string Name = "Product";
        }
    }
}
namespace netcore.Models
{
    public partial class ApplicationUser
    {
        [Display(Name = "Product")]
        public bool ProductRole { get; set; } = false;
    }
}



