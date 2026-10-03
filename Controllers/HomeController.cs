using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;

namespace WorkerBookingSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true
            && RoleLandingPolicy.GetDestination(
                User.IsInRole("Admin"), User.IsInRole("Worker"), User.IsInRole("Client")) is { } destination)
        {
            return RedirectToAction(destination.Action, destination.Controller);
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
