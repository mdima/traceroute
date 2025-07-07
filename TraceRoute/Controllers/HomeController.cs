//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Diagnostics;
//using Microsoft.AspNetCore.Mvc;
//using TraceRoute.Helpers;
//using TraceRoute.Models;

//namespace TraceRoute.Controllers
//{
//    public class HomeController(BogonIPService bogonIPService, ILoggerFactory LoggerFactory) : Controller
//    {        
//        private readonly BogonIPService _bogonIPService = bogonIPService;
//        private readonly ILogger _logger = LoggerFactory.CreateLogger<HomeController>();

//        public IActionResult Index()
//        {
//            if (Request != null && Request.HttpContext != null && Request.HttpContext.Connection.RemoteIpAddress != null) {
//                string clientIPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
//                if (! _bogonIPService.IsBogonIP(clientIPAddress))
//                {
//                    ViewData["ClientIPAddress"] = clientIPAddress;
//                }
//            }
//            // If the version is not available, set it to "Unknown"
//            Version? currentVersion = GetType()?.Assembly?.GetName()?.Version;
//            if (currentVersion != null)
//            {
//                ViewData["Version"] = string.Format("{0}.{1}.{2}", currentVersion.Major, currentVersion.Minor, currentVersion.Build);
//            }
//            else
//            {
//                ViewData["Version"] = "Unknown";
//            }
//            return View();
//        }

//        [HttpGet("/error")]
//        public IActionResult Error(int? statusCode = null)
//        {
//            string? RequestID = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
//            ErrorViewModel result = new() { RequestId = RequestID ?? "" };

//            if (HttpContext != null)
//            {
//                IStatusCodeReExecuteFeature? exceptionHandlerPathFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
//                if (exceptionHandlerPathFeature != null)
//                {
//                    _logger.LogError("Error code: {0}, path: {1}, querystring: {2}", statusCode, exceptionHandlerPathFeature.OriginalPath, exceptionHandlerPathFeature.OriginalQueryString);
//                    result.OriginalPath = exceptionHandlerPathFeature.OriginalPath;
//                }
//                else
//                {
//                    _logger.LogError("Error code: {0}", statusCode);
//                }
//            }
            
//            if (statusCode != null) { result.StatusCode = statusCode.Value; }

//            return View(result);
//        }
//    }
//}
