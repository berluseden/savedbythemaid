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
    [Authorize(Roles = "Warehouse")]
    public class WarehouseController : Controller
    {
        private readonly IWarehouseServices _warehouseServices;
        private readonly IBranchServices _branchServices;

        public WarehouseController(IWarehouseServices warehouseServices, IBranchServices branchServices)
        {
            _warehouseServices = warehouseServices;
            _branchServices = branchServices;
        }

        // GET: Warehouse
        public async Task<IActionResult> Index()
        {
            var result = await _warehouseServices.GetAllAsync();
            return View(result.IsSuccess ? result.Value : new List<WarehouseOutputDto>());
        }

        // GET: Warehouse/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _warehouseServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: Warehouse/Create
        public async Task<IActionResult> Create()
        {
            var branchesResult = await _branchServices.GetComboBoxAsync();
            var branches = branchesResult.IsSuccess ? branchesResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["branchId"] = new SelectList(branches, "StringId", "Name");
            return View();
        }

        // POST: Warehouse/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WarehouseInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _warehouseServices.SaveAsync(input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", result.Error);
            }
            
            var branchesResult = await _branchServices.GetComboBoxAsync();
            var branches = branchesResult.IsSuccess ? branchesResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["branchId"] = new SelectList(branches, "StringId", "Name", input.BranchId);
            return View(input);
        }

        // GET: Warehouse/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _warehouseServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var warehouse = result.Value;
            var input = new WarehouseInputDto
            {
                BranchId = warehouse.BranchId,
                WarehouseName = warehouse.WarehouseName,
                Description = warehouse.Description,
                Street1 = warehouse.Street1,
                Street2 = warehouse.Street2,
                City = warehouse.City,
                Province = warehouse.Province,
                Country = warehouse.Country
            };
            
            var branchesResult = await _branchServices.GetComboBoxAsync();
            var branches = branchesResult.IsSuccess ? branchesResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["branchId"] = new SelectList(branches, "StringId", "Name", warehouse.BranchId);
            ViewData["WarehouseId"] = id;
            return View(input);
        }

        // POST: Warehouse/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, WarehouseInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _warehouseServices.UpdateByIdAsync(id, input);
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
            
            var branchesResult = await _branchServices.GetComboBoxAsync();
            var branches = branchesResult.IsSuccess ? branchesResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["branchId"] = new SelectList(branches, "StringId", "Name", input.BranchId);
            ViewData["WarehouseId"] = id;
            return View(input);
        }

        // GET: Warehouse/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _warehouseServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: Warehouse/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var result = await _warehouseServices.DeleteAsync(id);
            if (result.IsFailure)
            {
                var warehouseResult = await _warehouseServices.GetByIdAsync(id);
                if (warehouseResult.IsSuccess)
                {
                    ViewData["StatusMessage"] = "Error. Calm Down ^_^ and please contact your SysAdmin with this message: " + result.Error;
                    return View(warehouseResult.Value);
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
        public static class Warehouse
        {
            public const string Controller = "Warehouse";
            public const string Action = "Index";
            public const string Role = "Warehouse";
            public const string Url = "/Warehouse/Index";
            public const string Name = "Warehouse";
        }
    }
}
namespace netcore.Models
{
    public partial class ApplicationUser
    {
        [Display(Name = "Warehouse")]
        public bool WarehouseRole { get; set; } = false;
    }
}



