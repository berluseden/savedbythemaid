using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EService.Web.Models;
using JempSoft.Applications.Services;

namespace EService.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICleaningPlaceServices _cleaningPlaceServices;
        private readonly IServiceTypeServices _serviceTypes;

        public HomeController(ICleaningPlaceServices cleaningPlaceServices, IServiceTypeServices serviceTypes)
        {
            _cleaningPlaceServices = cleaningPlaceServices;
            _serviceTypes = serviceTypes;
        }

        public IActionResult Index()
        {
            return View();
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



        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
