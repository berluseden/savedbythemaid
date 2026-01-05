using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using JempSoft.Applications;
using JempSoft.Applications.Services;
using netcore.Data;
using netcore.VMs;

namespace netcore.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeServices _employeeService;

        public EmployeesController(ApplicationDbContext context, IEmployeeServices employeeService)
        {
            _context = context;
            _employeeService = employeeService;
        }

        // GET: Employees
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
            
            var result = await _employeeService.GetAllAsync();
            if (result.IsFailure)
            {
                ViewBag.ErrorMessage = result.Error;
                return View(new List<Employee>());
            }
            return View(result.Value);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _employeeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var employee = result.Value;
            var schedules = await _context.EmployeeSchedules
                            .Include(e => e.Employee)
                            .Where(e => e.EmployeeId == id && e.AvaliableDay >= DateTime.Now)
                            .OrderBy(e => e.AvaliableDay).ToListAsync();

            var vm = new EmployeeScheduleVM
            {
                Employee = employee,
                EmployeeSchedule = schedules
            };

            return View(vm);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,FirstName,LastName,Identification,Address,ContactNumber,EmailAddress")] Employee employee)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employee.FirstName))
                {
                    ViewBag.ErrorMessage = "El nombre es requerido.";
                    return View(employee);
                }
                
                if (string.IsNullOrWhiteSpace(employee.LastName))
                {
                    ViewBag.ErrorMessage = "Los apellidos son requeridos.";
                    return View(employee);
                }

                // Validar email duplicado solo si se proporciona
                if (!string.IsNullOrWhiteSpace(employee.EmailAddress))
                {
                    var isEmailExist = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmailAddress == employee.EmailAddress);
                    
                    if (isEmailExist != null)
                    {
                        ViewBag.ErrorMessage = "Este email ya está siendo utilizado por otra empleada.";
                        return View(employee);
                    }
                }

                // Obtener el usuario actual logueado por email
                int? userId = null;
                var currentUserEmail = User.Identity?.Name;
                if (!string.IsNullOrEmpty(currentUserEmail))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserEmail);
                    if (user != null)
                    {
                        userId = user.UserId;
                    }
                }

                var input = new EmployeeInputDto
                {
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Identification = employee.Identification ?? string.Empty,
                    Address = employee.Address ?? string.Empty,
                    ContactNumber = employee.ContactNumber ?? string.Empty,
                    EmailAddress = employee.EmailAddress ?? string.Empty,
                    UserId = userId,
                    IsActive = true
                };

                var result = await _employeeService.SaveAsync(input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(employee);
                }
                
                TempData["SuccessMessage"] = $"Empleada '{employee.FirstName} {employee.LastName}' registrada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
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

            var result = await _employeeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }
            return View(result.Value);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,UserId,FirstName,LastName,Identification,Address,ContactNumber,EmailAddress,IsActive")] Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(employee.FirstName))
                {
                    ViewBag.ErrorMessage = "El nombre es requerido.";
                    return View(employee);
                }

                var input = new EmployeeInputDto
                {
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Identification = employee.Identification ?? string.Empty,
                    Address = employee.Address ?? string.Empty,
                    ContactNumber = employee.ContactNumber ?? string.Empty,
                    EmailAddress = employee.EmailAddress ?? string.Empty,
                    UserId = employee.UserId,
                    IsActive = employee.IsActive
                };

                var result = await _employeeService.UpdateByIdAsync(id, input);
                if (result.IsFailure)
                {
                    ViewBag.ErrorMessage = result.Error;
                    return View(employee);
                }
                
                TempData["SuccessMessage"] = $"Empleada '{employee.FirstName} {employee.LastName}' actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_employeeService.Exists(employee.EmployeeId))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message);
                return View(employee);
            }
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _employeeService.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var getResult = await _employeeService.GetByIdAsync(id);
                if (getResult.IsSuccess)
                {
                    var employeeName = $"{getResult.Value.FirstName} {getResult.Value.LastName}";
                    var deleteResult = await _employeeService.DeleteAsync(id);
                    if (deleteResult.IsFailure)
                    {
                        TempData["ErrorMessage"] = deleteResult.Error;
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Empleada '{employeeName}' eliminada exitosamente.";
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
