using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using netcore.Data;
using netcore.VMs;

namespace netcore.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Employees.Include(e => e.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.User)
                .SingleOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            var schedules = await _context.EmployeeSchedules
                            .Include(e => e.Employee)
                            .Where(e => e.EmployeeId == id && e.AvaliableDay >= DateTime.Now)
                            .OrderBy(e => e.AvaliableDay).ToListAsync();

            var result = new EmployeeScheduleVM
            {
                Employee = employee,
                EmployeeSchedule = schedules
            };

            return View(result);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserName");
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,UserId,FirstName,LastName,Identification,Address,ContactNumber,EmailAddress")] Employee employee)
        {
            try
            {
                var isEmailExist = _context.Employees.FirstOrDefault(e => e.EmailAddress.Contains(employee.EmailAddress));
                var userExist = _context.Employees.FirstOrDefault(u => u.UserId == employee.UserId);



                if(isEmailExist != null)
                {
                    ViewBag.HasError = true;
                    ViewBag.ErrorMessage = "Este email ya esta siendo utilizado.";

                    ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserName", employee.UserId);
                    return View(employee);
                }


                if (userExist != null)
                {
                    ViewBag.HasError = true;
                    ViewBag.ErrorMessage = "Este usuario ya esta registrado.";

                    ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserName", employee.UserId);
                    return View(employee);
                }


                if (ModelState.IsValid)
                {
                    _context.Add(employee);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserName", employee.UserId);
                return View(employee);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.InnerException.Message;

                ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserName", employee.UserId);
                return View(employee);
            }

        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.SingleOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserName", employee.UserId);
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,UserId,FirstName,LastName,Identification,Address,ContactNumber,EmailAddress")] Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId))
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
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserName", employee.UserId);
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.User)
                .SingleOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.SingleOrDefaultAsync(m => m.EmployeeId == id);
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }
    }
}
