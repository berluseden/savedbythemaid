using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using netcore.Data;

namespace netcore.Controllers
{
    public class ServiceMeetsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceMeetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ServiceMeets
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ServiceMeeting.Include(s => s.CartItem);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ServiceMeets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceMeet = await _context.ServiceMeeting
                .Include(s => s.CartItem)
                .SingleOrDefaultAsync(m => m.ServiceMeetId == id);
            if (serviceMeet == null)
            {
                return NotFound();
            }

            return View(serviceMeet);
        }

        // GET: ServiceMeets/Create
        public IActionResult Create()
        {
            ViewData["CartItemId"] = new SelectList(_context.CartItems, "CartItemId", "CartItemId");
            return View();
        }

        // POST: ServiceMeets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceMeetId,CartItemId,Title,Address,Day,Month,Year,Hour,Minute,isMorning")] ServiceMeet serviceMeet)
        {
            if (ModelState.IsValid)
            {
                _context.Add(serviceMeet);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CartItemId"] = new SelectList(_context.CartItems, "CartItemId", "CartItemId", serviceMeet.CartItemId);
            return View(serviceMeet);
        }

        // GET: ServiceMeets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceMeet = await _context.ServiceMeeting.SingleOrDefaultAsync(m => m.ServiceMeetId == id);
            if (serviceMeet == null)
            {
                return NotFound();
            }
            ViewData["CartItemId"] = new SelectList(_context.CartItems, "CartItemId", "CartItemId", serviceMeet.CartItemId);
            return View(serviceMeet);
        }

        // POST: ServiceMeets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceMeetId,CartItemId,Title,Address,Day,Month,Year,Hour,Minute,isMorning")] ServiceMeet serviceMeet)
        {
            if (id != serviceMeet.ServiceMeetId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceMeet);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceMeetExists(serviceMeet.ServiceMeetId))
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
            ViewData["CartItemId"] = new SelectList(_context.CartItems, "CartItemId", "CartItemId", serviceMeet.CartItemId);
            return View(serviceMeet);
        }

        // GET: ServiceMeets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceMeet = await _context.ServiceMeeting
                .Include(s => s.CartItem)
                .SingleOrDefaultAsync(m => m.ServiceMeetId == id);
            if (serviceMeet == null)
            {
                return NotFound();
            }

            return View(serviceMeet);
        }

        // POST: ServiceMeets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceMeet = await _context.ServiceMeeting.SingleOrDefaultAsync(m => m.ServiceMeetId == id);
            _context.ServiceMeeting.Remove(serviceMeet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceMeetExists(int id)
        {
            return _context.ServiceMeeting.Any(e => e.ServiceMeetId == id);
        }
    }
}
