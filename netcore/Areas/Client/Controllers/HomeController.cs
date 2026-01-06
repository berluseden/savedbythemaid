using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using netcore.Models;
using JempSoft.Applications.Services;

namespace netcore.Areas.Client.Controllers
{
    [Area("Client")]
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

        public IActionResult Services()
        {
            return View();
        }

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
