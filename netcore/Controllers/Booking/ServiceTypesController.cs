using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using netcore.Data;
using JempSoft.Applications.Administration.Page;

namespace netcore.Controllers
{
    public class ServiceTypesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPageCookieService _pageCookie;

        public ServiceTypesController(ApplicationDbContext context, IPageCookieService pageCookie)
        {
            _context = context;
            _pageCookie = pageCookie;
        }

        // GET: ServiceTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.ServiceTypes.ToListAsync());
        }

        // GET: ServiceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceType = await _context.ServiceTypes
                .SingleOrDefaultAsync(m => m.ServiceTypeId == id);
            if (serviceType == null)
            {
                return NotFound();
            }

            return View(serviceType);
        }

        // GET: ServiceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ServiceTypes/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceTypeId,Title,Cost,Price,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] ServiceType serviceType)
        {
            if (ModelState.IsValid)
            {
                serviceType.CreatorUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                serviceType.CreationDate = DateTime.UtcNow;

                _context.Add(serviceType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(serviceType);
        }

        // GET: ServiceTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceType = await _context.ServiceTypes.SingleOrDefaultAsync(m => m.ServiceTypeId == id);
            if (serviceType == null)
            {
                return NotFound();
            }
            return View(serviceType);
        }

        // POST: ServiceTypes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceTypeId,Title,Cost,Price,IsActive,CreatorUserId,CreationDate,UpdateUserId,UpdateDate,DeleteUserId,DeleteDate,IsDeleted")] ServiceType serviceType)
        {
            if (id != serviceType.ServiceTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    serviceType.UpdateUserId = Convert.ToInt32(_pageCookie.GetCookie("UserId"));
                    serviceType.UpdateDate = DateTime.UtcNow;

                    _context.Update(serviceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceTypeExists(serviceType.ServiceTypeId))
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
            return View(serviceType);
        }

        // GET: ServiceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceType = await _context.ServiceTypes
                .SingleOrDefaultAsync(m => m.ServiceTypeId == id);
            if (serviceType == null)
            {
                return NotFound();
            }

            return View(serviceType);
        }

        // POST: ServiceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceType = await _context.ServiceTypes.SingleOrDefaultAsync(m => m.ServiceTypeId == id);
            _context.ServiceTypes.Remove(serviceType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceTypeExists(int id)
        {
            return _context.ServiceTypes.Any(e => e.ServiceTypeId == id);
        }

        [HttpGet, ActionName("GetServiceTypeById")]
        public JsonResult GetById(int? id) {

            if(id.HasValue)
            {
                var result = _context.ServiceTypes.FirstOrDefault(c => c.ServiceTypeId == id.Value);
                return Json(new { Status = true, Data = result });
            }
            return Json(new { Status = false });
        }

    }
}
