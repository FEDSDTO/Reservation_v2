using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Reservation.Controllers
{
    public class DeliveryController : Controller
    {
        // GET: Delivery
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Order_meal()
        {
            return View();
        }

        public ActionResult Mb_order_meal()
        {
            return View();
        }

        public ActionResult Checkout()
        {
            return View();
        }

        public ActionResult Mb_checkout()
        {
            return View();
        }

    }
}