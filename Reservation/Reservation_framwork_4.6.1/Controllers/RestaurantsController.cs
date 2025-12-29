using Reservation.Models;
using Reservation.ViewModels.Restaurants;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Reservation.Controllers
{
    public class RestaurantsController : Controller
    {
        Restaurants _fun = new Restaurants();

        // GET: Restaurants
        public ActionResult Index(string id)
        {
            List<RestaurantCard> cards = new List<RestaurantCard>();
            if (!string.IsNullOrWhiteSpace(id))
                cards = _fun.GetRestaurants(id);
            return View(new RestaurantsIndex(cards));
        }
        
        public ActionResult Restaurants_filter()
        {
            return View();
        }

        public ActionResult Mall_filter()
        {
            MallFilter model = new MallFilter()
            {
                Malls = _fun.GetMalls()
            };

            return View(model);
        }
        
        public ActionResult MallLocationFilter()
        {
            MallFilter model = new MallFilter()
            {
                Malls = _fun.GetMalls()
            };

            return View(model);
        }

        public ActionResult Restaurants_Index()
        {
            TempData["ActivityTime"] = "Error";

            return View();
        }



    }
}