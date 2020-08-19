using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using netcore.Data;
using netcore.VMs;
using JempSoft.Core.Models.Administration;

namespace netcore.Controllers.Administration
{
    public class EmployeeSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeSchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: EmployeeSchedules
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.EmployeeSchedules.Include(e => e.Employee);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: EmployeeSchedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeSchedule = await _context.EmployeeSchedules
                .Include(e => e.Employee)
                .SingleOrDefaultAsync(m => m.EmployeeScheduleId == id);
            if (employeeSchedule == null)
            {
                return NotFound();
            }

            return View(employeeSchedule);
        }

        // GET: EmployeeSchedules/Create
        public async Task<IActionResult> Create(int? id)
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
                            .Where(e => e.EmployeeId == id).ToListAsync();

            var schedule = new EmployeeSchedule
            {
                EmployeeId = employee.EmployeeId,
                AvaliableDay = DateTime.Now
            };

            var result = new EmployeeScheduleVM
            {
                Employee = employee,
                EmployeeSchedule = schedules,
                Schedule = schedule
            };

            return View(result);
        }

        // POST: EmployeeSchedules/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeScheduleVM employeeSchedule)
        {
            var employeeId = employeeSchedule.Schedule.EmployeeId;
            if (ModelState.IsValid)
            {
                try
                {
                    if(employeeSchedule.EntireYear)
                    {
                        var day = employeeSchedule.Schedule.AvaliableDay;
                        while(day.Year == DateTime.Now.Year)
                        {


                            var programmedDay = _context.EmployeeSchedules.FirstOrDefault(a => a.AvaliableDay.DayOfYear == employeeSchedule.Schedule.AvaliableDay.DayOfYear);

                            if (programmedDay == null)
                            {
                                var emploSchedule = new EmployeeSchedule
                                {
                                    AvaliableDay = employeeSchedule.Schedule.AvaliableDay,
                                    EmployeeId = employeeSchedule.Schedule.EmployeeId
                                };

                                _context.Add(emploSchedule);

                                var avaliable = _context.AvaliableMaids.FirstOrDefault(c => c.DayOfAvaliability.DayOfYear == emploSchedule.AvaliableDay.DayOfYear);

                                if(avaliable == null)
                                {
                                    var avaliableDay = new AvaliableMaid
                                    {
                                        AvaliableCount = 1,
                                        ServiceCount = 0,
                                        DayOfAvaliability = employeeSchedule.Schedule.AvaliableDay
                                    };

                                    _context.Add(avaliableDay);
                                }
                                else
                                {
                                    avaliable.AvaliableCount += 1;
                                    _context.Entry(avaliable).State = EntityState.Modified;
                                }

                                await _context.SaveChangesAsync();
                            }

                            day = day.AddDays(1);
                            employeeSchedule.Schedule.AvaliableDay = day;
                        }                        
                    }
                    else
                    {
                        var programmedDay = _context.EmployeeSchedules.FirstOrDefault(a => a.AvaliableDay.ToShortDateString() == employeeSchedule.Schedule.AvaliableDay.ToShortDateString());
                        if(programmedDay == null)
                        {
                            _context.Add(employeeSchedule.Schedule);
                            await _context.SaveChangesAsync();
                        }
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                        throw;
                }

                 
                return RedirectToAction("Details", "Employees", new { id = employeeId });

            }

            return View(employeeSchedule);
        }

        // GET: EmployeeSchedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeSchedule = await _context.EmployeeSchedules.SingleOrDefaultAsync(m => m.EmployeeScheduleId == id);
            if (employeeSchedule == null)
            {
                return NotFound();
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName", employeeSchedule.EmployeeId);
            return View(employeeSchedule);
        }

        // POST: EmployeeSchedules/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeScheduleId,EmployeeId,AvaliableDay")] EmployeeSchedule employeeSchedule)
        {
            if (id != employeeSchedule.EmployeeScheduleId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employeeSchedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeScheduleExists(employeeSchedule.EmployeeScheduleId))
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
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName", employeeSchedule.EmployeeId);
            return View(employeeSchedule);
        }

        // GET: EmployeeSchedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeSchedule = await _context.EmployeeSchedules
                .Include(e => e.Employee)
                .SingleOrDefaultAsync(m => m.EmployeeScheduleId == id);
            if (employeeSchedule == null)
            {
                return NotFound();
            }

            return View(employeeSchedule);
        }

        // POST: EmployeeSchedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {


            var employeeSchedule = await _context.EmployeeSchedules.SingleOrDefaultAsync(m => m.EmployeeScheduleId == id);

            var employeeId = employeeSchedule.EmployeeId;
            _context.EmployeeSchedules.Remove(employeeSchedule);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Employees");

        }

        private bool EmployeeScheduleExists(int id)
        {
            return _context.EmployeeSchedules.Any(e => e.EmployeeScheduleId == id);
        }
    }
}
