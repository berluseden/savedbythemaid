using System.Threading.Tasks;
using netcore.Services;
using Microsoft.AspNetCore.Mvc;

namespace netcore.Areas.Client.Controllers
{
    [Area("Client")]
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
                    return RedirectToAction("Index", "Home", new { area = "Client" });
                }

                return View(result.Value);
            }
            return RedirectToAction("Login", "Account", new { area = "Client", returnUrl = "/Cart/Index/" });
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
