using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using netcore.Models;
using netcore.Data;
using netcore.VMs;
using netcore.Dto;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using netcore.Services.Administration.Page;
using netcore.Services.Services;

namespace netcore.Controllers.Booking
{
    public class CleaningPlaceRoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICleaningPlaceRoomServices _cleaningPlaceRoomService;
        private readonly IPageCookieService _pageCookie;

        public CleaningPlaceRoomsController(
            ApplicationDbContext context, 
            ICleaningPlaceRoomServices cleaningPlaceRoomService,
            IPageCookieService pageCookie)
        {
            _context = context;
            _cleaningPlaceRoomService = cleaningPlaceRoomService;
            _pageCookie = pageCookie;
        }

        // GET: CleaningPlaceRooms
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
            
            var result = await _cleaningPlaceRoomService.GetAllAsync();
            if (result.IsFailure)
            {
                ViewBag.ErrorMessage = result.Error;
                return View(new List<CleaningPlaceRoom>());
            }
            return View(result.Value);
        }

        // GET: CleaningPlaceRooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceRoomService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: CleaningPlaceRooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CleaningPlaceRooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CleaningPlaceRoomId,Title,IsActive")] CleaningPlaceRoom cleaningPlaceRoom)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cleaningPlaceRoom.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    return View(cleaningPlaceRoom);
                }

                var input = new CleaningPlaceRoomInputDto
                {
                    Title = cleaningPlaceRoom.Title,
                    IsActive = cleaningPlaceRoom.IsActive,
                    CreateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"))
                };

                var result = await _cleaningPlaceRoomService.SaveAsync(input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(cleaningPlaceRoom);
                }
                
                TempData["SuccessMessage"] = $"Tipo de habitación '{cleaningPlaceRoom.Title}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
                return View(cleaningPlaceRoom);
            }
        }

        // GET: CleaningPlaceRooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceRoomService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }
            return View(result.Value);
        }

        // POST: CleaningPlaceRooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CleaningPlaceRoomId,Title,IsActive")] CleaningPlaceRoom cleaningPlaceRoom)
        {
            if (id != cleaningPlaceRoom.CleaningPlaceRoomId)
            {
                return NotFound();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(cleaningPlaceRoom.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    return View(cleaningPlaceRoom);
                }

                var input = new CleaningPlaceRoomInputDto
                {
                    CleaningPlaceRoomId = cleaningPlaceRoom.CleaningPlaceRoomId,
                    Title = cleaningPlaceRoom.Title,
                    IsActive = cleaningPlaceRoom.IsActive,
                    CreateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"))
                };

                var result = await _cleaningPlaceRoomService.UpdateByIdAsync(id, input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(cleaningPlaceRoom);
                }
                
                TempData["SuccessMessage"] = $"Tipo de habitación '{cleaningPlaceRoom.Title}' actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_cleaningPlaceRoomService.Exists(cleaningPlaceRoom.CleaningPlaceRoomId))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
                return View(cleaningPlaceRoom);
            }
        }

        // GET: CleaningPlaceRooms/AddServiceTypeToPlaceRoom/5
        public async Task<IActionResult> AddServiceTypeToPlaceRoom(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceRoomService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var cleaningPlaceRoom = result.Value;
            var serviceTypeAddeds = _context.CleaningPlaceRoomServiceTypes.ToList().Where(c => c.CleaningPlaceRoomId == id);

            var serviceTypes = _context.ServiceTypes.ToList();
            var serviceTypesAdded = new List<ServiceType>();

            foreach (var item in serviceTypeAddeds)
            {
                var serviceType = _context.ServiceTypes.FirstOrDefault(c => c.ServiceTypeId == item.ServiceTypeId);

                serviceTypesAdded.Add(serviceType);
                serviceTypes.Remove(serviceType);
            }

            var placeRoomServiceTypeVM = new PlaceRoomsServiceTypesVM
            {
                CleaningPlaceRooms = cleaningPlaceRoom,
                ServiceTypes = serviceTypes,
                ServiceTypesddeds = serviceTypesAdded
            };

            ViewBag.ServiceTypes = new SelectList(placeRoomServiceTypeVM.ServiceTypes, "ServiceTypeId", "FullDescription");
                       
            return View(placeRoomServiceTypeVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddServiceToPlaceRoom(PlaceRoomsServiceTypesVM input)
        {
            if (input.CleaningPlaceRooms == null)
            {
                return NotFound();
            }

            if (input.ServiceTypesIds.Length <= 0)
            {
                var cleaningPlaceRoom = _context.CleaningPlaceRooms.FirstOrDefault(c => c.CleaningPlaceRoomId == input.CleaningPlaceRooms.CleaningPlaceRoomId);
                return View(cleaningPlaceRoom);
            }

            try
            {
                for (var i = 0; i < input.ServiceTypesIds.Length; i++)
                {
                    var placeRoomServiceType = _context.CleaningPlaceRoomServiceTypes.FirstOrDefault(c => c.CleaningPlaceRoomId == input.CleaningPlaceRooms.CleaningPlaceRoomId
                                                                                                    && c.ServiceTypeId == input.ServiceTypesIds[i]);

                    if (placeRoomServiceType == null)
                    {
                        var entity = new CleaningPlaceRoomServiceType
                        {
                            CleaningPlaceRoomId = input.CleaningPlaceRooms.CleaningPlaceRoomId,
                            ServiceTypeId = input.ServiceTypesIds[i]
                        };

                        _context.Add(entity);
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // GET: CleaningPlaceRooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceRoomService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: CleaningPlaceRooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var getResult = await _cleaningPlaceRoomService.GetByIdAsync(id);
                if (getResult.IsSuccess)
                {
                    var title = getResult.Value.Title;
                    var deleteResult = await _cleaningPlaceRoomService.DeleteAsync(id);
                    if (deleteResult.IsFailure)
                    {
                        TempData["ErrorMessage"] = deleteResult.Error;
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Tipo de habitación '{title}' eliminado exitosamente.";
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
