using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models.Services;
using netcore.Data;
using JempSoft.Applications.Administration.Page;

namespace netcore.Controllers.Booking
{
    public class AdditionalServiceTypesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPageCookieService _pageCookie;

        public AdditionalServiceTypesController(ApplicationDbContext context, IPageCookieService pageCookie)
        {
            _context = context;
            _pageCookie = pageCookie;
        }

        // GET: AdditionalServiceTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.AdditionalServiceTypes.ToListAsync());
        }

        // GET: AdditionalServiceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var additionalServiceType = await _context.AdditionalServiceTypes
                .SingleOrDefaultAsync(m => m.AdditionalServiceTypeId == id);
            if (additionalServiceType == null)
            {
                return NotFound();
            }

            return View(additionalServiceType);
        }

        // GET: AdditionalServiceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdditionalServiceTypes/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AdditionalServiceTypeId,Title,Cost,Price,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] AdditionalServiceType additionalServiceType)
        {
            if (ModelState.IsValid)
            {
                additionalServiceType.CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                additionalServiceType.CreationDate = DateTime.UtcNow;

                _context.Add(additionalServiceType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(additionalServiceType);
        }

        // GET: AdditionalServiceTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var additionalServiceType = await _context.AdditionalServiceTypes.SingleOrDefaultAsync(m => m.AdditionalServiceTypeId == id);
            if (additionalServiceType == null)
            {
                return NotFound();
            }
            return View(additionalServiceType);
        }

        // POST: AdditionalServiceTypes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AdditionalServiceTypeId,Title,Cost,Price,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] AdditionalServiceType additionalServiceType)
        {
            if (id != additionalServiceType.AdditionalServiceTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    additionalServiceType.UpdateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                    additionalServiceType.UpdateDate = DateTime.UtcNow;

                    _context.Update(additionalServiceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdditionalServiceTypeExists(additionalServiceType.AdditionalServiceTypeId))
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
            return View(additionalServiceType);
        }

        // GET: AdditionalServiceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var additionalServiceType = await _context.AdditionalServiceTypes
                .SingleOrDefaultAsync(m => m.AdditionalServiceTypeId == id);
            if (additionalServiceType == null)
            {
                return NotFound();
            }

            return View(additionalServiceType);
        }

        // POST: AdditionalServiceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var additionalServiceType = await _context.AdditionalServiceTypes.SingleOrDefaultAsync(m => m.AdditionalServiceTypeId == id);
            _context.AdditionalServiceTypes.Remove(additionalServiceType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AdditionalServiceTypeExists(int id)
        {
            return _context.AdditionalServiceTypes.Any(e => e.AdditionalServiceTypeId == id);
        }
    }
}
