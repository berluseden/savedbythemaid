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
using netcore.Data;
using netcore.VMs;

namespace netcore.Controllers.Administration
{
    public class EmployeeSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeScheduleServices _employeeScheduleService;
        private readonly IEmployeeServices _employeeService;

        public EmployeeSchedulesController(
            ApplicationDbContext context, 
            IEmployeeScheduleServices employeeScheduleService,
            IEmployeeServices employeeService)
        {
            _context = context;
            _employeeScheduleService = employeeScheduleService;
            _employeeService = employeeService;
        }

        // GET: EmployeeSchedules
        public async Task<IActionResult> Index()
        {
            var result = await _employeeScheduleService.GetAllAsync();
            if (result.IsFailure)
            {
                ViewBag.ErrorMessage = result.Error;
                return View(new List<EmployeeSchedule>());
            }
            return View(result.Value);
        }

        // GET: EmployeeSchedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _employeeScheduleService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: EmployeeSchedules/Create
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeResult = await _employeeService.GetByIdAsync(id.Value);
            if (employeeResult.IsFailure)
            {
                return NotFound();
            }

            var employee = employeeResult.Value;
            
            var schedulesResult = await _employeeScheduleService.GetByEmployeeIdAsync(id.Value);
            var schedules = schedulesResult.IsSuccess ? schedulesResult.Value : new List<EmployeeSchedule>();

            var schedule = new EmployeeSchedule
            {
                EmployeeId = employee.EmployeeId,
                AvaliableDay = DateTime.Now
            };

            var vm = new EmployeeScheduleVM
            {
                Employee = employee,
                EmployeeSchedule = schedules,
                Schedule = schedule
            };

            return View(vm);
        }

        // POST: EmployeeSchedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeScheduleVM employeeSchedule)
        {
            if (employeeSchedule?.Schedule == null)
            {
                ModelState.AddModelError("", "Schedule information is required.");
                return RedirectToAction("Index", "Employees");
            }

            var employeeId = employeeSchedule.Schedule.EmployeeId;
            
            // Helper method to reload view model
            async Task<EmployeeScheduleVM> ReloadViewModel()
            {
                var employeeResult = await _employeeService.GetByIdAsync(employeeId);
                var employee = employeeResult.IsSuccess ? employeeResult.Value : null;
                    
                var schedulesResult = await _employeeScheduleService.GetByEmployeeIdAsync(employeeId);
                var schedules = schedulesResult.IsSuccess ? schedulesResult.Value : new List<EmployeeSchedule>();
                    
                return new EmployeeScheduleVM
                {
                    Employee = employee,
                    EmployeeSchedule = schedules,
                    Schedule = employeeSchedule.Schedule,
                    EntireYear = employeeSchedule.EntireYear
                };
            }
            
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
                                var input = new EmployeeScheduleInputDto
                                {
                                    EmployeeId = employeeSchedule.Schedule.EmployeeId,
                                    AvaliableDay = employeeSchedule.Schedule.AvaliableDay,
                                    IsActive = true
                                };

                                await _employeeScheduleService.SaveAsync(input);

                                var avaliable = _context.AvaliableMaids.FirstOrDefault(c => c.DayOfAvaliability.DayOfYear == input.AvaliableDay.DayOfYear);

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
                            var input = new EmployeeScheduleInputDto
                            {
                                EmployeeId = employeeSchedule.Schedule.EmployeeId,
                                AvaliableDay = employeeSchedule.Schedule.AvaliableDay,
                                IsActive = true
                            };
                            await _employeeScheduleService.SaveAsync(input);
                        }
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }

                return RedirectToAction("Details", "Employees", new { id = employeeId });
            }

            var reloadedModel = await ReloadViewModel();
            return View(reloadedModel);
        }

        // GET: EmployeeSchedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _employeeScheduleService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName", result.Value.EmployeeId);
            return View(result.Value);
        }

        // POST: EmployeeSchedules/Edit/5
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
                    var input = new EmployeeScheduleInputDto
                    {
                        EmployeeId = employeeSchedule.EmployeeId,
                        AvaliableDay = employeeSchedule.AvaliableDay,
                        IsActive = true
                    };

                    var result = await _employeeScheduleService.UpdateByIdAsync(id, input);
                    if (result.IsFailure)
                    {
                        ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName", employeeSchedule.EmployeeId);
                        ViewBag.ErrorMessage = result.Error;
                        return View(employeeSchedule);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_employeeScheduleService.Exists(employeeSchedule.EmployeeScheduleId))
                    {
                        return NotFound();
                    }
                    throw;
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

            var result = await _employeeScheduleService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: EmployeeSchedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _employeeScheduleService.DeleteAsync(id);
            return RedirectToAction("Index", "Employees");
        }
    }
}
