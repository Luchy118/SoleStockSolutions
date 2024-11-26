using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    public class CheckoutController : Controller
    {
        public ActionResult Cart()
        {
            ViewBag.CurrentAction = "Cart";
            return View();
        }

        public ActionResult Index()
        {
            ViewBag.CurrentAction = "Checkout";
            return View();
        }
    }
}