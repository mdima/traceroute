using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TraceRoute.Helpers;
using TraceRoute.Models;

namespace TraceRoute.Controllers
{
    public class HomeController(BogonIPService bogonIPService) : Controller
    {        
        private readonly BogonIPService _bogonIPService = bogonIPService;

        public IActionResult Index()
        {
            if (Request != null && Request.Host != null && Request.Host.HasValue) {
                string clientIPAddress = Request.Host.ToString();
                if (! _bogonIPService.IsBogonIP(clientIPAddress))
                {
                    ViewData["ClientIPAddress"] = clientIPAddress;
                }
            }

            ViewData["GoogleMapsAPIKey"] = ConfigurationHelper.GetGoogleMapsAPIKey();
            return View();
        }

        [HttpGet("/about")]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return PartialView();
        }

        public IActionResult Error()
        {
            string? RequestID = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;

            return View(new ErrorViewModel { RequestId = RequestID ?? ""});
        }
    }
}
