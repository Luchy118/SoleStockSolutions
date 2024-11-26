using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    public class InformationController : Controller
    {
        public ActionResult About()
        {
            ViewBag.CurrentAction = "About";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.CurrentAction = "Contact";
            return View();
        }
    }
}