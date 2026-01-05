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
    [Authorize(Roles = "Branch")]
    public class BranchController : Controller
    {
        private readonly IBranchServices _branchServices;

        public BranchController(IBranchServices branchServices)
        {
            _branchServices = branchServices;
        }

        // GET: Branch
        public async Task<IActionResult> Index()
        {
            var result = await _branchServices.GetAllAsync();
            return View(result.IsSuccess ? result.Value : new List<BranchOutputDto>());
        }

        // GET: Branch/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _branchServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: Branch/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Branch/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _branchServices.SaveAsync(input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", result.Error);
            }
            return View(input);
        }

        // GET: Branch/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _branchServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var branch = result.Value;
            var input = new BranchInputDto
            {
                BranchName = branch.BranchName,
                Description = branch.Description,
                Street1 = branch.Street1,
                Street2 = branch.Street2,
                City = branch.City,
                Province = branch.Province,
                Country = branch.Country,
                IsDefaultBranch = branch.IsDefaultBranch
            };
            ViewData["BranchId"] = id;
            return View(input);
        }

        // POST: Branch/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, BranchInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _branchServices.UpdateByIdAsync(id, input);
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
            ViewData["BranchId"] = id;
            return View(input);
        }

        // GET: Branch/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _branchServices.GetByIdAsync(id);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: Branch/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var result = await _branchServices.DeleteAsync(id);
            if (result.IsFailure)
            {
                var branchResult = await _branchServices.GetByIdAsync(id);
                if (branchResult.IsSuccess)
                {
                    ViewData["StatusMessage"] = "Error. Calm Down ^_^ and please contact your SysAdmin with this message: " + result.Error;
                    return View(branchResult.Value);
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
        public static class Branch
        {
            public const string Controller = "Branch";
            public const string Action = "Index";
            public const string Role = "Branch";
            public const string Url = "/Branch/Index";
            public const string Name = "Branch";
        }
    }
}
namespace netcore.Models
{
    public partial class ApplicationUser
    {
        [Display(Name = "Branch")]
        public bool BranchRole { get; set; } = false;
    }
}



