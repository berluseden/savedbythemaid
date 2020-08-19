using JempSoft.Applications;
using JempSoft.Applications.Administration.Page;
using JempSoft.Applications.Book.Dto;
using JempSoft.Applications.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EService.Web.Controllers
{
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
        public JsonResult GetAllCleaningPlace()
        {
            var result = _cleaningPlaceServices.GetComboBoxOutPut();

            return Json(new { CleaningPlaces = result });
        }

        [HttpGet]
        public JsonResult GetRoomsByCleaningPlaceId(int? id)
        {
            var result = _cleaningPlaceRoomServices.GetComboBoxByCleaningPlaceId(id);

            return Json(new { CleaningPlaceRooms = result });
        }

        [HttpGet]
        public JsonResult GetServiceTypeByPlaceRoomsId(int? id)
        {
            var result = _serviceTypeServices.GetComboBoxByCleaningPlaceRoomId(id);

            return Json(new { CleaningPlaceRooms = result });
        }

        [HttpGet]
        public JsonResult HasPendingOrders(string email)
        {
            var result = _serviceTypeServices.GetComboBoxByCleaningPlaceRoomId(1);

            return Json(new { CleaningPlaceRooms = result });
        }

        [HttpPost]
        public async Task<IActionResult> Book(BookInputDto input)
        {
            var cartItem = await _bookingService.Book(input);


            return Json(new { CartItemId = cartItem.CartItemId });
        }

        public IActionResult ProcessToCheckOut(int id)
        {
            int value;

            if (User.Identity.IsAuthenticated)
            {

                var item = _bookingService.ProcessToCheckOut(id);
                if (item != null)
                {
                    return View(item);

                }
                return NotFound();

            }
            else
            {
                return RedirectToAction("Login", "Account", new { returnUrl = string.Format("/Booking/ProcessToCheckOut?id=" + id) });
            }



        }

        [HttpGet]
        public JsonResult GetAvaliableMaidByMonth(string date)
        {
            var day = Convert.ToDateTime(date);

            var avaliableMaids = _bookingService.GetAvaliableMaidThisMonth(new DateTime(year: day.Year, month: day.Month, day: day.Day));

            return Json(new { AvaliableMaids = avaliableMaids });
        }

        [HttpGet]
        public JsonResult GetAvaliableMaidByDay(int year, int month, int day)
        {
            var avaliableMaids = _bookingService.GetAvaliablesMaidsByDay(new DateTime(year: year, month: month, day: day));

            return Json(new { AvaliableMaids = avaliableMaids });
        }

        //cartItemId: cartItem.value, day: day, month: month, year: year, hour: hour, minute: minute 

        [HttpPost]
        public JsonResult AddToCart(int cartItemId, int day, int month, int year, int hour, int minute, List<int> aditionalServices, ServiceContactInfoInputDto contactInfo)
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
                Email = User.Identity.Name
            };

            contactInfo.Email = User.Identity.Name;

            if (User.Identity.IsAuthenticated)
            {
                long serviceOrderId;

                var isAddToCart = _bookingService.AddToCart(serviceOrder, aditionalServices, contactInfo, out serviceOrderId);

                if (isAddToCart != false)
                {
                    var cartServiceByUserName = _bookingService.GetCartServiceByUserName(User.Identity.Name);
                }

                return Json(new { IsAdded = true, IsAuthenticated = true, serviceOrderId, serviceOrder });
            }
            else
            {
                return Json(new { IsAdded = false, IsAuthenticated = false, ServiceOrder = serviceOrder, AdditionalServices = aditionalServices, ContactInfo = contactInfo });
            }           

        }

        /// <summary>
        /// Schedule
        /// </summary>
        /// <param name="id">CartItemId</param>
        /// <returns></returns>

        public IActionResult Schedule(int id)
        {
            return View();
        }
    }
}