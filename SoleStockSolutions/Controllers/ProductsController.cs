using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    public class ProductsController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new TFCEntities())
            {
                List<Inventario> listaProductosDisponibles = db.Inventario.GroupBy(i => i.id_producto).Select(g => g.FirstOrDefault()).ToList();
                ViewData["ListaProductosDisponibles"] = listaProductosDisponibles;
                ViewBag.CurrentAction = "Products";

                return View();
            }
        }

        public ActionResult ProductDetails(string productName)
        {
            ViewBag.ProductName = productName;
            return View();
        }

        public JsonResult GetMinMaxPrice()
        {
            using (var db = new TFCEntities())
            {
                var minPrice = db.Inventario.Min(i => i.precio);
                var maxPrice = db.Inventario.Max(i => i.precio);
                return Json(new { minPrice, maxPrice }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult FilterProducts(decimal? minPrice, decimal? maxPrice, List<int?> selectedBrands, int? modelId)
        {
            using (var db = new TFCEntities())
            {
                var products = db.Inventario.AsQueryable();

                if (minPrice.HasValue && maxPrice.HasValue)
                    products = products.Where(p => p.precio >= minPrice.Value && p.precio <= maxPrice.Value);

                if (selectedBrands != null && selectedBrands.Any())
                    products = products.Where(p => selectedBrands.Contains(p.id_marca));

                if (modelId.HasValue)
                {
                    var productIds = db.Productos.Where(p => p.id_modelo == modelId.Value).Select(p => p.id_producto).ToList();
                    products = products.Where(p => productIds.Contains(p.id_producto));
                }

                var filteredProducts = products.GroupBy(p => p.id_producto).Select(g => g.FirstOrDefault()).ToList();

                return PartialView("_ProductList", filteredProducts);
            }
        }

    }
}