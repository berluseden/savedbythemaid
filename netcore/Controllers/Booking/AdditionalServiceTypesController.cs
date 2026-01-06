using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using netcore.Services;
using netcore.Services.Administration.Page;
using netcore.Services.Services;

namespace netcore.Controllers.Booking
{
    public class AdditionalServiceTypesController : Controller
    {
        private readonly IAdditionalServiceTypeServices _additionalServiceTypeService;
        private readonly IPageCookieService _pageCookie;

        public AdditionalServiceTypesController(
            IAdditionalServiceTypeServices additionalServiceTypeService,
            IPageCookieService pageCookie)
        {
            _additionalServiceTypeService = additionalServiceTypeService;
            _pageCookie = pageCookie;
        }

        // GET: AdditionalServiceTypes
        public async Task<IActionResult> Index()
        {
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }
            
            var result = await _additionalServiceTypeService.GetAllAsync();
            if (result.IsFailure)
            {
                ViewBag.ErrorMessage = result.Error;
                return View(new List<AdditionalServiceType>());
            }
            return View(result.Value);
        }

        // GET: AdditionalServiceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _additionalServiceTypeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: AdditionalServiceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdditionalServiceTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AdditionalServiceTypeId,Title,Cost,Price,IsActive")] AdditionalServiceType additionalServiceType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(additionalServiceType.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    return View(additionalServiceType);
                }

                var input = new AdditionalServiceTypeInputDto
                {
                    Title = additionalServiceType.Title,
                    Cost = additionalServiceType.Cost,
                    Price = additionalServiceType.Price,
                    IsActive = additionalServiceType.IsActive,
                    CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"))
                };

                var result = await _additionalServiceTypeService.SaveAsync(input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(additionalServiceType);
                }
                
                TempData["SuccessMessage"] = $"Servicio adicional '{additionalServiceType.Title}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
                return View(additionalServiceType);
            }
        }

        // GET: AdditionalServiceTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _additionalServiceTypeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }
            return View(result.Value);
        }

        // POST: AdditionalServiceTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AdditionalServiceTypeId,Title,Cost,Price,IsActive")] AdditionalServiceType additionalServiceType)
        {
            if (id != additionalServiceType.AdditionalServiceTypeId)
            {
                return NotFound();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(additionalServiceType.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    return View(additionalServiceType);
                }

                var input = new AdditionalServiceTypeInputDto
                {
                    Title = additionalServiceType.Title,
                    Cost = additionalServiceType.Cost,
                    Price = additionalServiceType.Price,
                    IsActive = additionalServiceType.IsActive,
                    CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"))
                };

                var result = await _additionalServiceTypeService.UpdateByIdAsync(id, input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(additionalServiceType);
                }
                
                TempData["SuccessMessage"] = $"Servicio adicional '{additionalServiceType.Title}' actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_additionalServiceTypeService.Exists(additionalServiceType.AdditionalServiceTypeId))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
                return View(additionalServiceType);
            }
        }

        // GET: AdditionalServiceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _additionalServiceTypeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: AdditionalServiceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var getResult = await _additionalServiceTypeService.GetByIdAsync(id);
                if (getResult.IsSuccess)
                {
                    var title = getResult.Value.Title;
                    var deleteResult = await _additionalServiceTypeService.DeleteAsync(id);
                    if (deleteResult.IsFailure)
                    {
                        TempData["ErrorMessage"] = deleteResult.Error;
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Servicio adicional '{title}' eliminado exitosamente.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
