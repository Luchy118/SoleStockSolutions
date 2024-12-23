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
        /// <summary>
        /// Acción que devuelve la vista de la página "About".
        /// </summary>
        /// <returns>Vista de la página "About".</returns>
        public ActionResult About()
        {
            ViewBag.CurrentAction = "About";
            return View();
        }

        /// <summary>
        /// Acción que devuelve la vista de la página "Contact".
        /// </summary>
        /// <returns>Vista de la página "Contact".</returns>
        public ActionResult Contact()
        {
            ViewBag.CurrentAction = "Contact";
            return View();
        }
    }
}