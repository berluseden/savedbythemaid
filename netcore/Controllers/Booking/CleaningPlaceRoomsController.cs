 using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using netcore.Data;
using netcore.VMs;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using JempSoft.Applications.Administration.Page;

namespace netcore.Controllers.Booking
{
    public class CleaningPlaceRoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPageCookieService _pageCookie;

        public CleaningPlaceRoomsController(ApplicationDbContext context, IPageCookieService pageCookie)
        {
            _context = context;
            _pageCookie = pageCookie;
        }

        // GET: CleaningPlaceRooms
        public async Task<IActionResult> Index()
        {
            return View(await _context.CleaningPlaceRooms.ToListAsync());
        }

        // GET: CleaningPlaceRooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cleaningPlaceRoom = await _context.CleaningPlaceRooms
                .SingleOrDefaultAsync(m => m.CleaningPlaceRoomId == id);
            if (cleaningPlaceRoom == null)
            {
                return NotFound();
            }

            return View(cleaningPlaceRoom);
        }

        // GET: CleaningPlaceRooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CleaningPlaceRooms/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CleaningPlaceRoomId,Title,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] CleaningPlaceRoom cleaningPlaceRoom)
        {
            if (ModelState.IsValid)
            {
                cleaningPlaceRoom.CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                cleaningPlaceRoom.CreationDate = DateTime.UtcNow;

                _context.Add(cleaningPlaceRoom);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cleaningPlaceRoom);
        }

        // GET: CleaningPlaceRooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cleaningPlaceRoom = await _context.CleaningPlaceRooms.SingleOrDefaultAsync(m => m.CleaningPlaceRoomId == id);
            if (cleaningPlaceRoom == null)
            {
                return NotFound();
            }
            return View(cleaningPlaceRoom);
        }

        // POST: CleaningPlaceRooms/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CleaningPlaceRoomId,Title,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] CleaningPlaceRoom cleaningPlaceRoom)
        {
            if (id != cleaningPlaceRoom.CleaningPlaceRoomId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    cleaningPlaceRoom.UpdateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                    cleaningPlaceRoom.UpdateDate = DateTime.UtcNow;

                    _context.Update(cleaningPlaceRoom);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CleaningPlaceRoomExists(cleaningPlaceRoom.CleaningPlaceRoomId))
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
            return View(cleaningPlaceRoom);
        }

        // GET: CleaningPlaceRooms/Edit/5
        public async Task<IActionResult> AddServiceTypeToPlaceRoom(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cleaningPlaceRoom = await _context.CleaningPlaceRooms.SingleOrDefaultAsync(m => m.CleaningPlaceRoomId == id);


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

            if (cleaningPlaceRoom == null)
            {
                return NotFound();
            }

            ViewBag.ServiceTypes = new SelectList(placeRoomServiceTypeVM.ServiceTypes, "ServiceTypeId", "FullDescription");
                       
            return View(placeRoomServiceTypeVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // GET: CleaningPlaces/Edit/5
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

            var cleaningPlaceRoom = await _context.CleaningPlaceRooms
                .SingleOrDefaultAsync(m => m.CleaningPlaceRoomId == id);
            if (cleaningPlaceRoom == null)
            {
                return NotFound();
            }

            return View(cleaningPlaceRoom);
        }

        // POST: CleaningPlaceRooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cleaningPlaceRoom = await _context.CleaningPlaceRooms.SingleOrDefaultAsync(m => m.CleaningPlaceRoomId == id);
            _context.CleaningPlaceRooms.Remove(cleaningPlaceRoom);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CleaningPlaceRoomExists(int id)
        {
            return _context.CleaningPlaceRooms.Any(e => e.CleaningPlaceRoomId == id);
        }
    }
}
