using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using netcore.Models;
using netcore.Services;
using netcore.Services.Services;
using netcore.Services.Administration.Page;

namespace netcore.Controllers
{
    public class ServiceTypesController : Controller
    {
        private readonly IServiceTypeServices _serviceTypeService;
        private readonly IPageCookieService _pageCookie;

        public ServiceTypesController(IServiceTypeServices serviceTypeService, IPageCookieService pageCookie)
        {
            _serviceTypeService = serviceTypeService;
            _pageCookie = pageCookie;
        }

        // GET: ServiceTypes
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

            var result = await _serviceTypeService.GetAllAsync();
            if (result.IsFailure)
            {
                ViewBag.ErrorMessage = result.Error;
                return View(new List<ServiceType>());
            }

            return View(result.Value);
        }

        // GET: ServiceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _serviceTypeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: ServiceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ServiceTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceTypeInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    return View(MapToServiceType(input));
                }

                input.CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                input.IsActive = true;

                var result = await _serviceTypeService.SaveAsync(input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(MapToServiceType(input));
                }

                TempData["SuccessMessage"] = $"Tipo de servicio '{input.Title}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + ex.Message;
                return View(MapToServiceType(input));
            }
        }

        // GET: ServiceTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _serviceTypeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: ServiceTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceTypeInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    var entity = await _serviceTypeService.GetByIdAsync(id);
                    return View(entity.IsSuccess ? entity.Value : MapToServiceType(input, id));
                }

                input.CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));

                var result = await _serviceTypeService.UpdateByIdAsync(id, input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    var entity = await _serviceTypeService.GetByIdAsync(id);
                    return View(entity.IsSuccess ? entity.Value : MapToServiceType(input, id));
                }

                TempData["SuccessMessage"] = $"Tipo de servicio '{input.Title}' actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_serviceTypeService.Exists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + ex.Message;
                var entity = await _serviceTypeService.GetByIdAsync(id);
                return View(entity.IsSuccess ? entity.Value : MapToServiceType(input, id));
            }
        }

        // GET: ServiceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _serviceTypeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: ServiceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var entity = await _serviceTypeService.GetByIdAsync(id);
                var title = entity.IsSuccess ? entity.Value.Title : "";

                var result = await _serviceTypeService.DeleteAsync(id);
                if (result.IsFailure)
                {
                    TempData["ErrorMessage"] = result.Error;
                }
                else
                {
                    TempData["SuccessMessage"] = $"Tipo de servicio '{title}' eliminado exitosamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet, ActionName("GetServiceTypeById")]
        public async Task<JsonResult> GetById(int? id)
        {
            if (id.HasValue)
            {
                var result = await _serviceTypeService.GetByIdAsync(id.Value);
                if (result.IsSuccess)
                {
                    return Json(new { Status = true, Data = result.Value });
                }
            }
            return Json(new { Status = false });
        }

        #region Private Helpers

        private static ServiceType MapToServiceType(ServiceTypeInputDto input, int? id = null)
        {
            return new ServiceType
            {
                ServiceTypeId = id ?? 0,
                Title = input.Title,
                Cost = input.Cost,
                Price = input.Price,
                IsActive = input.IsActive
            };
        }

        #endregion
    }
}
