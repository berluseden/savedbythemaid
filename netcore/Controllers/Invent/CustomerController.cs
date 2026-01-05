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
using netcore.Data;

namespace netcore.Controllers.Invent
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ICustomerServices _customerServices;
        private readonly ApplicationDbContext _context; // Kept for cascade delete of CustomerLine

        public CustomerController(ICustomerServices customerServices, ApplicationDbContext context)
        {
            _customerServices = customerServices;
            _context = context;
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            var result = await _customerServices.GetAllAsync();
            return View(result.IsSuccess ? result.Value : new List<CustomerOutputDto>());
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _customerServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _customerServices.SaveAsync(input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Details), new { id = result.Value });
                }
                ModelState.AddModelError("", result.Error);
            }
            return View(input);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _customerServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var customer = result.Value;
            var input = new CustomerInputDto
            {
                CustomerName = customer.CustomerName,
                Description = customer.Description,
                Size = customer.Size,
                Street1 = customer.Street1,
                Street2 = customer.Street2,
                City = customer.City,
                Province = customer.Province,
                Country = customer.Country
            };
            ViewData["CustomerId"] = id;
            return View(input);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CustomerInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _customerServices.UpdateByIdAsync(id, input);
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
            ViewData["CustomerId"] = id;
            return View(input);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _customerServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                // First delete related CustomerLines
                var customerLines = await _context.CustomerLine.Where(c => c.customerId == id).ToListAsync();
                if (customerLines.Any())
                {
                    _context.CustomerLine.RemoveRange(customerLines);
                    await _context.SaveChangesAsync();
                }

                var result = await _customerServices.DeleteAsync(id);
                if (result.IsFailure)
                {
                    var customerResult = await _customerServices.GetByIdAsync(id);
                    if (customerResult.IsSuccess)
                    {
                        ViewData["StatusMessage"] = "Error. Calm Down ^_^ and please contact your SysAdmin with this message: " + result.Error;
                        return View(customerResult.Value);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var customerResult = await _customerServices.GetByIdAsync(id);
                if (customerResult.IsSuccess)
                {
                    ViewData["StatusMessage"] = "Error. Calm Down ^_^ and please contact your SysAdmin with this message: " + ex.Message;
                    return View(customerResult.Value);
                }
                return RedirectToAction(nameof(Index));
            }
        }
    }
}





namespace netcore.MVC
{
    public static partial class Pages
    {
        public static class Customer
        {
            public const string Controller = "Customer";
            public const string Action = "Index";
            public const string Role = "Customer";
            public const string Url = "/Customer/Index";
            public const string Name = "Customer";
        }
    }
}
namespace netcore.Models
{
    public partial class ApplicationUser
    {
        [Display(Name = "Customer")]
        public bool CustomerRole { get; set; } = false;
    }
}



