using SoleStockSolutions.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
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