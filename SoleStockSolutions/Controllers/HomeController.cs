using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Acción que maneja la solicitud para la página de inicio.
        /// Carga los modelos relevantes y establece la acción actual en ViewBag.
        /// </summary>
        /// <returns>Vista de la página de inicio.</returns>
        [LoadModelosRelevantes]
        public ActionResult Index()
        {
            ViewBag.CurrentAction = "HomeIndex";
            return View();
        }
    }
}