using JempSoft.Applications;
using JempSoft.Applications.Administration.Page;
using JempSoft.Applications.Book.Dto;
using JempSoft.Applications.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace netcore.Areas.Client.Controllers
{
    [Area("Client")]
    public class BookingController : Controller
    {
        private readonly ICleaningPlaceServices _cleaningPlaceServices;
        private readonly ICleaningPlaceRoomServices _cleaningPlaceRoomServices;
        private readonly IServiceTypeServices _serviceTypeServices;
        private readonly IBookingService _bookingService;
        private readonly IPageCookieService _pageCookieService;

        public BookingController(ICleaningPlaceServices cleaningPlaceServices,
                                 ICleaningPlaceRoomServices cleaningPlaceRoomServices,
                                 IServiceTypeServices serviceTypeServices,
                                 IBookingService bookingService,
                                 IPageCookieService pageCookieService)
        {
            _cleaningPlaceServices = cleaningPlaceServices;
            _cleaningPlaceRoomServices = cleaningPlaceRoomServices;
            _serviceTypeServices = serviceTypeServices;
            _bookingService = bookingService;
            _pageCookieService = pageCookieService;
        }

        [HttpGet]
        public async Task<JsonResult> GetAllCleaningPlace()
        {
            var result = await _cleaningPlaceServices.GetComboBoxOutputAsync();
            return Json(new { CleaningPlaces = result.IsSuccess ? result.Value : new List<ComboBoxOutPutDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetRoomsByCleaningPlaceId(int? id)
        {
            if (!id.HasValue)
                return Json(new { CleaningPlaceRooms = new List<ComboBoxOutPutDto>() });
            
            var result = await _cleaningPlaceRoomServices.GetComboBoxByCleaningPlaceIdAsync(id.Value);
            return Json(new { CleaningPlaceRooms = result.IsSuccess ? result.Value : new List<ComboBoxOutPutDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> GetServiceTypeByPlaceRoomsId(int? id)
        {
            if (!id.HasValue)
                return Json(new { CleaningPlaceRooms = new List<ComboBoxOutPutDto>() });
            
            var result = await _serviceTypeServices.GetComboBoxByCleaningPlaceRoomIdAsync(id.Value);
            return Json(new { CleaningPlaceRooms = result.IsSuccess ? result.Value : new List<ComboBoxOutPutDto>() });
        }

        [HttpGet]
        public async Task<JsonResult> HasPendingOrders(string email)
        {
            var result = await _serviceTypeServices.GetComboBoxByCleaningPlaceRoomIdAsync(1);
            return Json(new { CleaningPlaceRooms = result.IsSuccess ? result.Value : new List<ComboBoxOutPutDto>() });
        }

        [HttpPost]
        public async Task<IActionResult> Book(BookInputDto input)
        {
            var result = await _bookingService.BookAsync(input);
            if (result.IsFailure)
                return BadRequest(new { Error = result.Error });
            
            return Json(new { CartItemId = result.Value.CartItemId });
        }

        public async Task<IActionResult> ProcessToCheckOut(int id)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var result = await _bookingService.ProcessToCheckOutAsync(id);
                if (result.IsSuccess)
                {
                    return View(result.Value);
                }
                return NotFound();
            }
            else
            {
                return RedirectToAction("Login", "Account", new { area = "Client", returnUrl = $"/Booking/ProcessToCheckOut?id={id}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvaliableMaidByMonth(string date)
        {
            var day = Convert.ToDateTime(date);
            var result = await _bookingService.GetAvailableMaidThisMonthAsync(new DateTime(year: day.Year, month: day.Month, day: day.Day));
            return Json(new { AvaliableMaids = result.IsSuccess ? result.Value : new AvaliableMaidMonthOutputDto() });
        }

        [HttpGet]
        public async Task<JsonResult> GetAvaliableMaidByDay(int year, int month, int day)
        {
            var result = await _bookingService.GetAvailableMaidsByDayAsync(new DateTime(year: year, month: month, day: day));
            return Json(new { AvaliableMaids = result.IsSuccess ? result.Value : 0 });
        }

        [HttpPost]
        public async Task<JsonResult> AddToCart(int cartItemId, int day, int month, int year, int hour, int minute, List<int> aditionalServices, ServiceContactInfoInputDto contactInfo)
        {
            var serviceOrder = new ServiceOrderInputDto
            {
                CartItemId = cartItemId,
                Day = day,
                Month = month,
                Year = year,
                Hour = hour,
                Minute = minute,
                IsActive = true,
                IsComplete = false,
                IsPayed = false,
                Email = User.Identity?.Name ?? string.Empty
            };

            contactInfo.Email = User.Identity?.Name ?? string.Empty;

            if (User.Identity?.IsAuthenticated == true)
            {
                var result = await _bookingService.AddToCartAsync(serviceOrder, aditionalServices, contactInfo);
                
                if (result.IsSuccess)
                {
                    await _bookingService.GetCartServiceByUserNameAsync(User.Identity.Name!);
                }

                return Json(new { IsAdded = result.IsSuccess, IsAuthenticated = true, serviceOrderId = result.IsSuccess ? result.Value : 0, serviceOrder });
            }
            else
            {
                return Json(new { IsAdded = false, IsAuthenticated = false, ServiceOrder = serviceOrder, AdditionalServices = aditionalServices, ContactInfo = contactInfo });
            }           
        }

        public IActionResult Schedule(int id)
        {
            return View();
        }
    }
}
