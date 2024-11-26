using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.CurrentAction = "HomeIndex";
            return View();
        }
    }
}