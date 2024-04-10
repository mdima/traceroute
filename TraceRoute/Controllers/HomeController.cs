﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TraceRoute.Helpers;
using TraceRoute.Models;

namespace TraceRoute.Controllers
{
    public class HomeController(BogonIPService bogonIPService, ILoggerFactory LoggerFactory) : Controller
    {        
        private readonly BogonIPService _bogonIPService = bogonIPService;
        private readonly ILogger _logger = LoggerFactory.CreateLogger<HomeController>();

        public IActionResult Index()
        {
            if (Request != null && Request.HttpContext != null && Request.HttpContext.Connection.RemoteIpAddress != null) {
                string clientIPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                if (! _bogonIPService.IsBogonIP(clientIPAddress))
                {
                    ViewData["ClientIPAddress"] = clientIPAddress;
                }
            }
            return View();
        }

        [HttpGet("/about")]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return PartialView();
        }

        [HttpGet("/error")]
        public IActionResult Error(int? statusCode = null)
        {
            if (HttpContext != null)
            { 
                IExceptionHandlerPathFeature? exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                if (exceptionHandlerPathFeature != null)
                    _logger.LogError(exceptionHandlerPathFeature?.Error, "Error");
                else
                    _logger.LogError("Error code: {0}", statusCode);
            }
            string? RequestID = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
            ErrorViewModel result = new() { RequestId = RequestID ?? "" };
            if (statusCode != null) { result.StatusCode = statusCode.Value; }

            return View(result);
        }
    }
}
