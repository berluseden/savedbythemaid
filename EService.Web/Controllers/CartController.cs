using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JempSoft.Applications;
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


        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartItems = _bookingService.GetCartServiceByUserName(User.Identity.Name);

                if(cartItems.Services.Count == 0)
                {
                    return RedirectToAction("Index", "Home");
                }

                return View(cartItems);

            }
            return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Index/" });
        }

        [HttpPost]
        public JsonResult DeleteItem(long id)
        {
            var resultMessage = "";

            var data = _bookingService.RemoveItemOnCart(id, out resultMessage);

            if(data != null)
            {
                return Json(new { Item = data, IsDelete = true, ResultMessage = resultMessage });
            }

            return Json(new { Item = data, IsDelete = false, ResultMessage = resultMessage });
        }
    }
}