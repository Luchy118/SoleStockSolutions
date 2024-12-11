using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    public class AdminPanelController : Controller
    {
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult Index()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.CurrentAction = "AdminIndex";
                return View();
            }
        }

        [CustomAuthorize(Roles = "Admin")]
        public ActionResult Products()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.CurrentAction = "AdminProducts";
                var productos = db.Productos.ToList();
                return View(productos);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult CreateProduct(Productos producto)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    db.Productos.Add(producto);
                    db.SaveChanges();
                    return RedirectToAction("Products");
                }
                return View(producto);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult UpdateProduct(Productos producto)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    db.Entry(producto).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Products");
                }
                return View(producto);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult DeleteProduct(int id)
        {
            using (var db = new TFCEntities())
            {
                var producto = db.Productos.Find(id);
                if (producto != null)
                {
                    db.Productos.Remove(producto);
                    db.SaveChanges();
                }
                return RedirectToAction("Products");
            }
        }

        [CustomAuthorize(Roles = "Admin")]
        public JsonResult SearchProduct(string query)
        {
            using (var db = new TFCEntities())
            {
                var productos = db.Productos
                .Where(p => p.nombre.Contains(query))
                .Select(p => new { p.id_producto, p.nombre, ImageUrl = p.imagen })
                .ToList();
                return Json(productos, JsonRequestBehavior.AllowGet);
            }
        }

        [CustomAuthorize(Roles = "Admin")]
        public JsonResult GetProduct(string id)
        {
            using (var db = new TFCEntities())
            {
                var producto = db.Productos
                .Where(p => p.id_producto == id)
                .Select(p => new { p.id_producto, p.nombre, p.Marcas.nombre_marca, p.Modelos.nombre_modelo })
                .FirstOrDefault();
                return Json(producto, JsonRequestBehavior.AllowGet);
            }
        }
    }
}