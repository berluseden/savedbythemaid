using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using netcore.Models;
using netcore.Data;
using netcore.VMs;
using netcore.Dto;
using netcore.Services.Administration.Page;
using netcore.Services.Services;

namespace netcore.Controllers
{
    public class CleaningPlacesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICleaningPlaceServices _cleaningPlaceService;
        private readonly IPageCookieService _pageCookie;

        public CleaningPlacesController(
            ApplicationDbContext context, 
            ICleaningPlaceServices cleaningPlaceService,
            IPageCookieService pageCookie)
        {
            _context = context;
            _cleaningPlaceService = cleaningPlaceService;
            _pageCookie = pageCookie;
        }

        // GET: CleaningPlaces
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
            
            var result = await _cleaningPlaceService.GetAllAsync();
            if (result.IsFailure)
            {
                ViewBag.ErrorMessage = result.Error;
                return View(new List<CleaningPlace>());
            }
            return View(result.Value);
        }

        // GET: CleaningPlaces/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: CleaningPlaces/Create
        public IActionResult Create()
        {           
            return View();
        }

        // POST: CleaningPlaces/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CleaningPlaceId,Title,IsActive")] CleaningPlace cleaningPlace)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cleaningPlace.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    return View(cleaningPlace);
                }

                var input = new CleaningPlaceInputDto
                {
                    Title = cleaningPlace.Title,
                    IsActive = cleaningPlace.IsActive,
                    CreateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"))
                };

                var result = await _cleaningPlaceService.SaveAsync(input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(cleaningPlace);
                }
                
                TempData["SuccessMessage"] = $"Tipo de inmueble '{cleaningPlace.Title}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
                return View(cleaningPlace);
            }
        }

        // GET: CleaningPlaces/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var cleaningPlace = result.Value;
            
            // Obtener todas las habitaciones y las que ya están asociadas
            var allRooms = await _context.CleaningPlaceRooms.Where(r => r.IsActive).ToListAsync();
            var assignedRoomIds = await _context.CleaningPlaceCleaningPlaceRooms
                .Where(c => c.CleaningPlaceId == id.Value)
                .Select(c => c.CleaningPlaceRoomId)
                .ToListAsync();

            var viewModel = new CleaningPlaceEditVM
            {
                CleaningPlaceId = cleaningPlace.CleaningPlaceId,
                Title = cleaningPlace.Title,
                IsActive = cleaningPlace.IsActive,
                CreatorUserId = cleaningPlace.CreatorUserId,
                CreationDate = cleaningPlace.CreationDate,
                AvailableRooms = allRooms.Select(r => new RoomSelectionItem
                {
                    CleaningPlaceRoomId = r.CleaningPlaceRoomId,
                    Title = r.Title,
                    IsActive = r.IsActive,
                    IsSelected = assignedRoomIds.Contains(r.CleaningPlaceRoomId)
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: CleaningPlaces/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CleaningPlaceEditVM viewModel, int[] SelectedRoomIds)
        {
            if (id != viewModel.CleaningPlaceId)
            {
                return NotFound();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(viewModel.Title))
                {
                    ViewBag.ErrorMessage = "El título es requerido.";
                    return await ReloadEditView(viewModel);
                }

                // Actualizar el tipo de inmueble
                var input = new CleaningPlaceInputDto
                {
                    CleaningPlaceId = viewModel.CleaningPlaceId,
                    Title = viewModel.Title,
                    IsActive = viewModel.IsActive,
                    CreateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"))
                };

                var result = await _cleaningPlaceService.UpdateByIdAsync(id, input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return await ReloadEditView(viewModel);
                }

                // Actualizar las habitaciones asociadas
                var existingRooms = await _context.CleaningPlaceCleaningPlaceRooms
                    .Where(c => c.CleaningPlaceId == id)
                    .ToListAsync();
                
                // Eliminar las que ya no están seleccionadas
                var roomsToRemove = existingRooms.Where(e => !SelectedRoomIds.Contains(e.CleaningPlaceRoomId)).ToList();
                _context.CleaningPlaceCleaningPlaceRooms.RemoveRange(roomsToRemove);

                // Agregar las nuevas seleccionadas
                var existingRoomIds = existingRooms.Select(e => e.CleaningPlaceRoomId).ToList();
                var roomsToAdd = SelectedRoomIds.Where(r => !existingRoomIds.Contains(r)).ToList();
                foreach (var roomId in roomsToAdd)
                {
                    _context.CleaningPlaceCleaningPlaceRooms.Add(new CleaningPlaceCleaningPlaceRoom
                    {
                        CleaningPlaceId = id,
                        CleaningPlaceRoomId = roomId
                    });
                }

                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Tipo de inmueble '{viewModel.Title}' actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_cleaningPlaceService.Exists(viewModel.CleaningPlaceId))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
                return await ReloadEditView(viewModel);
            }
        }

        private async Task<IActionResult> ReloadEditView(CleaningPlaceEditVM viewModel)
        {
            var allRooms = await _context.CleaningPlaceRooms.Where(r => r.IsActive).ToListAsync();
            var assignedRoomIds = await _context.CleaningPlaceCleaningPlaceRooms
                .Where(c => c.CleaningPlaceId == viewModel.CleaningPlaceId)
                .Select(c => c.CleaningPlaceRoomId)
                .ToListAsync();

            viewModel.AvailableRooms = allRooms.Select(r => new RoomSelectionItem
            {
                CleaningPlaceRoomId = r.CleaningPlaceRoomId,
                Title = r.Title,
                IsActive = r.IsActive,
                IsSelected = assignedRoomIds.Contains(r.CleaningPlaceRoomId)
            }).ToList();

            return View(viewModel);
        }

        // GET: CleaningPlaces/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: CleaningPlaces/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var getResult = await _cleaningPlaceService.GetByIdAsync(id);
                if (getResult.IsSuccess)
                {
                    var title = getResult.Value.Title;
                    var deleteResult = await _cleaningPlaceService.DeleteAsync(id);
                    if (deleteResult.IsFailure)
                    {
                        TempData["ErrorMessage"] = deleteResult.Error;
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Tipo de inmueble '{title}' eliminado exitosamente.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: CleaningPlaces/AddPlaceRoom/5
        public async Task<IActionResult> AddPlaceRoom(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _cleaningPlaceService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var cleaningPlace = result.Value;
            var roomAddeds = _context.CleaningPlaceCleaningPlaceRooms.ToList().Where(c => c.CleaningPlaceId == id);

            var cleaningPlaceRooms = _context.CleaningPlaceRooms.ToList();
            var cleanginPlaceRommsAdded = new List<CleaningPlaceRoom>();

            foreach (var item in roomAddeds)
            {
                var cleaningPlaceRoom = _context.CleaningPlaceRooms.FirstOrDefault(c => c.CleaningPlaceRoomId == item.CleaningPlaceRoomId);

                cleanginPlaceRommsAdded.Add(cleaningPlaceRoom);
                cleaningPlaceRooms.Remove(item.CleaningPlaceRoom);
            }

            var cleaningPlaceAndRoomsVM = new CleaningPlacePlaceRoomsVM
            {
                CleaningPlace = cleaningPlace,
                CleaningPlaceRooms = cleaningPlaceRooms,
                CleaningPlaceRoomAddeds = cleanginPlaceRommsAdded.ToList()
            };

            ViewBag.CleaningPlaceRooms = new SelectList(cleaningPlaceAndRoomsVM.CleaningPlaceRooms, "CleaningPlaceRoomId", "Title");

            return View(cleaningPlaceAndRoomsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPlaceRoom(CleaningPlacePlaceRoomsVM input)
        {
            if (input.CleaningPlace == null)
            {
                return NotFound();
            }
            if(input.CleaningPlaceRoomIds == null)
            {
                ViewBag.ErrorMessage = "Debe agregar al menos un item!";
                return NotFound();
            }
            
            if (input.CleaningPlaceRoomIds.Length <= 0)
            {
                var cleaningPlace = _context.CleaningPlaces.FirstOrDefault(c => c.CleaningPlaceId == input.CleaningPlace.CleaningPlaceId);
                return View(cleaningPlace);
            }

            try
            {
                for(var i = 0; i < input.CleaningPlaceRoomIds.Length; i++)
                {
                    var cleaningPlaceRoom = _context.CleaningPlaceCleaningPlaceRooms.FirstOrDefault(c => c.CleaningPlaceId == input.CleaningPlace.CleaningPlaceId
                                                                                                    && c.CleaningPlaceRoomId == input.CleaningPlaceRoomIds[i]);

                    if(cleaningPlaceRoom == null)
                    {
                        var entity = new CleaningPlaceCleaningPlaceRoom
                        {
                            CleaningPlaceId = input.CleaningPlace.CleaningPlaceId,
                            CleaningPlaceRoomId = input.CleaningPlaceRoomIds[i]
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
    }
}