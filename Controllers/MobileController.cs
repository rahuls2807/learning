using Microsoft.AspNetCore.Mvc;

namespace WorkerBookingSystem.Controllers
{
    public class MobileController : Controller
    {
        public IActionResult Install()
        {
            return View();
        }

        public IActionResult Offline()
        {
            return View();
        }
    }
}
