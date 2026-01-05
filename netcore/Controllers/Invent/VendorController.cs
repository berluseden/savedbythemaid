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
    [Authorize(Roles = "Vendor")]
    public class VendorController : Controller
    {
        private readonly IVendorServices _vendorServices;
        private readonly ApplicationDbContext _context; // Kept for cascade delete of VendorLine

        public VendorController(IVendorServices vendorServices, ApplicationDbContext context)
        {
            _vendorServices = vendorServices;
            _context = context;
        }

        // GET: Vendor
        public async Task<IActionResult> Index()
        {
            var result = await _vendorServices.GetAllAsync();
            return View(result.IsSuccess ? result.Value : new List<VendorOutputDto>());
        }

        // GET: Vendor/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _vendorServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: Vendor/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Vendor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _vendorServices.SaveAsync(input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Details), new { id = result.Value });
                }
                ModelState.AddModelError("", result.Error);
            }
            return View(input);
        }

        // GET: Vendor/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _vendorServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var vendor = result.Value;
            var input = new VendorInputDto
            {
                VendorName = vendor.VendorName,
                Description = vendor.Description,
                Size = vendor.Size,
                Street1 = vendor.Street1,
                Street2 = vendor.Street2,
                City = vendor.City,
                Province = vendor.Province,
                Country = vendor.Country
            };
            ViewData["VendorId"] = id;
            return View(input);
        }

        // POST: Vendor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, VendorInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _vendorServices.UpdateByIdAsync(id, input);
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
            ViewData["VendorId"] = id;
            return View(input);
        }

        // GET: Vendor/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _vendorServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: Vendor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                // First delete related VendorLines
                var vendorLines = await _context.VendorLine.Where(v => v.vendorId == id).ToListAsync();
                if (vendorLines.Any())
                {
                    _context.VendorLine.RemoveRange(vendorLines);
                    await _context.SaveChangesAsync();
                }

                var result = await _vendorServices.DeleteAsync(id);
                if (result.IsFailure)
                {
                    var vendorResult = await _vendorServices.GetByIdAsync(id);
                    if (vendorResult.IsSuccess)
                    {
                        ViewData["StatusMessage"] = "Error. Calm Down ^_^ and please contact your SysAdmin with this message: " + result.Error;
                        return View(vendorResult.Value);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var vendorResult = await _vendorServices.GetByIdAsync(id);
                if (vendorResult.IsSuccess)
                {
                    ViewData["StatusMessage"] = "Error. Calm Down ^_^ and please contact your SysAdmin with this message: " + ex.Message;
                    return View(vendorResult.Value);
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
        public static class Vendor
        {
            public const string Controller = "Vendor";
            public const string Action = "Index";
            public const string Role = "Vendor";
            public const string Url = "/Vendor/Index";
            public const string Name = "Vendor";
        }
    }
}
namespace netcore.Models
{
    public partial class ApplicationUser
    {
        [Display(Name = "Vendor")]
        public bool VendorRole { get; set; } = false;
    }
}



