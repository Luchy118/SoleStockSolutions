using SoleStockSolutions.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    public class ErrorController : Controller
    {
        public ActionResult NotFound()
        {
            return View();
        }

        public ActionResult Unauthorized()
        {
            return View();
        }
    }
}