using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using netcore.Data;
using netcore.VMs;
using netcore.Extensions;
using JempSoft.Applications.Administration.Page;

namespace netcore.Controllers
{
    public class CleaningPlacesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPageCookieService _pageCookie;

        public CleaningPlacesController(ApplicationDbContext context, IPageCookieService pageCookie)
        {
            _context = context;
            _pageCookie = pageCookie;
        }

        // GET: CleaningPlaces
        public async Task<IActionResult> Index()
        {
            return View(await _context.CleaningPlaces.ToListAsync());
        }

        // GET: CleaningPlaces/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cleaningPlace = await _context.CleaningPlaces
                .SingleOrDefaultAsync(m => m.CleaningPlaceId == id);
            if (cleaningPlace == null)
            {
                return NotFound();
            }

            return View(cleaningPlace);
        }

        // GET: CleaningPlaces/Create
        public IActionResult Create()
        {           

            return View();
        }

        // POST: CleaningPlaces/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CleaningPlaceId,Title,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] CleaningPlace cleaningPlace)
        {
            if (ModelState.IsValid)
            {
                cleaningPlace.CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                cleaningPlace.CreationDate = DateTime.UtcNow;

                _context.Add(cleaningPlace);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cleaningPlace);
        }

        // GET: CleaningPlaces/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cleaningPlace = await _context.CleaningPlaces.SingleOrDefaultAsync(m => m.CleaningPlaceId == id);
            if (cleaningPlace == null)
            {
                return NotFound();
            }
            return View(cleaningPlace);
        }

        // POST: CleaningPlaces/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CleaningPlaceId,Title,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] CleaningPlace cleaningPlace)
        {
            if (id != cleaningPlace.CleaningPlaceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {

                    cleaningPlace.UpdateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                    cleaningPlace.UpdateDate = DateTime.UtcNow;

                    _context.Update(cleaningPlace);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CleaningPlaceExists(cleaningPlace.CleaningPlaceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cleaningPlace);
        }

        // GET: CleaningPlaces/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cleaningPlace = await _context.CleaningPlaces
                .SingleOrDefaultAsync(m => m.CleaningPlaceId == id);
            if (cleaningPlace == null)
            {
                return NotFound();
            }

            return View(cleaningPlace);
        }

        // POST: CleaningPlaces/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cleaningPlace = await _context.CleaningPlaces.SingleOrDefaultAsync(m => m.CleaningPlaceId == id);
            _context.CleaningPlaces.Remove(cleaningPlace);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CleaningPlaceExists(int id)
        {
            return _context.CleaningPlaces.Any(e => e.CleaningPlaceId == id);
        }

        // GET: CleaningPlaces/Edit/5
        public async Task<IActionResult> AddPlaceRoom(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cleaningPlace = await _context.CleaningPlaces.SingleOrDefaultAsync(m => m.CleaningPlaceId == id);
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

            if (cleaningPlace == null)
            {
                return NotFound();
            }

            ViewBag.CleaningPlaceRooms = new SelectList(cleaningPlaceAndRoomsVM.CleaningPlaceRooms, "CleaningPlaceRoomId", "Title");

            return View(cleaningPlaceAndRoomsVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // GET: CleaningPlaces/Edit/5
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