using Microsoft.AspNetCore.Mvc;

namespace Reservation.Controllers
{
    public class WaitingPositionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
