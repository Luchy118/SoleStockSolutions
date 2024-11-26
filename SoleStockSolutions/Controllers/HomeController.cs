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
        public ActionResult Index()
        {
            using (var db = new TFCEntities())
            {
                var modelos = db.Modelos
                    .Select(m => new
                    {
                        modelo = m,
                        vistas_ventas = m.Productos.Sum(p => p.veces_visto) + m.Productos.SelectMany(p => p.Inventario).Sum(i => i.veces_vendido)
                    })
                    .Where(m => m.vistas_ventas > 0)
                    .OrderBy(m => m.modelo.id_marca)
                    .ThenByDescending(m => m.vistas_ventas)
                    .Take(4)
                    .ToList();
                var modelosRelevantes = modelos.Select(m => m.modelo).ToList();

                Session["modelosRelevantes"] = modelosRelevantes;
                ViewBag.CurrentAction = "HomeIndex";
                return View();
            }
        }
    }
}