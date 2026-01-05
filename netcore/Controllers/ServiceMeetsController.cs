using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JempSoft.Core.Models;
using JempSoft.Applications.ServiceMeet;
using JempSoft.Applications.ServiceMeet.Dto;

namespace netcore.Controllers
{
    public class ServiceMeetsController : Controller
    {
        private readonly IServiceMeetServices _serviceMeetServices;

        public ServiceMeetsController(IServiceMeetServices serviceMeetServices)
        {
            _serviceMeetServices = serviceMeetServices;
        }

        // GET: ServiceMeets
        public async Task<IActionResult> Index()
        {
            var result = await _serviceMeetServices.GetAllAsync();
            if (result.IsFailure)
            {
                return View(new List<ServiceMeetOutputDto>());
            }
            return View(result.Value);
        }

        // GET: ServiceMeets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _serviceMeetServices.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // GET: ServiceMeets/Create
        public async Task<IActionResult> Create()
        {
            var cartItemsResult = await _serviceMeetServices.GetCartItemsComboBoxAsync();
            var cartItems = cartItemsResult.IsSuccess ? cartItemsResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["CartItemId"] = new SelectList(cartItems, "Id", "Name");
            return View();
        }

        // POST: ServiceMeets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceMeetInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _serviceMeetServices.SaveAsync(input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", result.Error);
            }
            
            var cartItemsResult = await _serviceMeetServices.GetCartItemsComboBoxAsync();
            var cartItems = cartItemsResult.IsSuccess ? cartItemsResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["CartItemId"] = new SelectList(cartItems, "Id", "Name", input.CartItemId);
            return View(input);
        }

        // GET: ServiceMeets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _serviceMeetServices.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            var serviceMeet = result.Value;
            var input = new ServiceMeetInputDto
            {
                CartItemId = serviceMeet.CartItemId,
                Title = serviceMeet.Title,
                Address = serviceMeet.Address,
                Day = serviceMeet.Day,
                Month = serviceMeet.Month,
                Year = serviceMeet.Year,
                Hour = serviceMeet.Hour,
                Minute = serviceMeet.Minute,
                IsMorning = serviceMeet.IsMorning
            };
            
            var cartItemsResult = await _serviceMeetServices.GetCartItemsComboBoxAsync();
            var cartItems = cartItemsResult.IsSuccess ? cartItemsResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["CartItemId"] = new SelectList(cartItems, "Id", "Name", serviceMeet.CartItemId);
            ViewData["ServiceMeetId"] = id.Value;
            return View(input);
        }

        // POST: ServiceMeets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceMeetInputDto input)
        {
            if (ModelState.IsValid)
            {
                var result = await _serviceMeetServices.UpdateByIdAsync(id, input);
                if (result.IsSuccess)
                {
                    return RedirectToAction(nameof(Index));
                }
                
                if (result.Error.Contains("not found"))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", result.Error);
            }
            
            var cartItemsResult = await _serviceMeetServices.GetCartItemsComboBoxAsync();
            var cartItems = cartItemsResult.IsSuccess ? cartItemsResult.Value : new List<JempSoft.Applications.ComboBoxOutPutDto>();
            ViewData["CartItemId"] = new SelectList(cartItems, "Id", "Name", input.CartItemId);
            ViewData["ServiceMeetId"] = id;
            return View(input);
        }

        // GET: ServiceMeets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _serviceMeetServices.GetByIdAsync(id.Value);
            if (result.IsFailure)
            {
                return NotFound();
            }

            return View(result.Value);
        }

        // POST: ServiceMeets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _serviceMeetServices.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
