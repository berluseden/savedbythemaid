using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JempSoft.Applications;
using JempSoft.Applications.Book.Dto;
using Microsoft.AspNetCore.Mvc;

namespace EService.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly IBookingService _bookingService;

        public CartController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var result = await _bookingService.GetCartServiceByUserNameAsync(User.Identity.Name!);
                
                if (result.IsFailure || result.Value.Services.Count == 0)
                {
                    return RedirectToAction("Index", "Home");
                }

                return View(result.Value);
            }
            return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Index/" });
        }

        [HttpPost]
        public async Task<JsonResult> DeleteItem(long id)
        {
            var result = await _bookingService.RemoveItemFromCartAsync(id);

            if (result.IsSuccess)
            {
                return Json(new { Item = result.Value, IsDelete = true, ResultMessage = "Item removed successfully" });
            }

            return Json(new { Item = (object?)null, IsDelete = false, ResultMessage = result.Error ?? "Error removing item" });
        }
    }
}