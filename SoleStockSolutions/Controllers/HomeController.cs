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
            using (var db = new TFCEntities())
            {
                var topViewedProducts = GetTopViewedProducts(db);
                var bestSellingProducts = GetBestSellingProducts(db, topViewedProducts);
                var topBrands = db.Marcas
                    .OrderByDescending(m => m.Productos.Count)
                    .Take(6)
                    .ToList();

                ViewBag.TopBrands = topBrands;
                ViewBag.TopViewedProducts = topViewedProducts;
                ViewBag.BestSellingProducts = bestSellingProducts;
                ViewBag.CurrentAction = "HomeIndex";

                if (User.Identity.IsAuthenticated)
                {
                    var userId = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name)?.id_usuario;
                    var wishlistProducts = db.Wishlist
                        .Where(w => w.id_usuario == userId)
                        .Select(w => w.Productos.id_producto)
                        .ToList();

                    ViewBag.WishlistProductsIds = wishlistProducts;
                }

                return View();
            }
        }

        /// <summary>
        /// Obtiene los productos más vistos, asegurándose de que no se repitan.
        /// </summary>
        /// <param name="db">Contexto de la base de datos.</param>
        /// <returns>Lista de productos más vistos.</returns>
        private List<Inventario> GetTopViewedProducts(TFCEntities db)
        {
            return db.Inventario
                     .Where(i => i.cantidad > 0)
                     .Join(db.Productos, inv => inv.id_producto, prod => prod.id_producto, (inv, prod) => new { inv, prod })
                     .GroupBy(x => x.inv.id_producto)
                     .Select(g => g.OrderByDescending(x => x.prod.veces_visto).FirstOrDefault().inv)
                     .OrderByDescending(inv => inv.Productos.veces_visto)
                     .Take(8)
                     .ToList();
        }

        /// <summary>
        /// Obtiene los productos más vendidos, excluyendo los productos más vistos y asegurándose de que no se repitan.
        /// </summary>
        /// <param name="db">Contexto de la base de datos.</param>
        /// <param name="topViewedProducts">Lista de productos más vistos.</param>
        /// <returns>Lista de productos más vendidos.</returns>
        private List<Inventario> GetBestSellingProducts(TFCEntities db, List<Inventario> topViewedProducts)
        {
            var topViewedProductIds = topViewedProducts.Select(p => p.id_producto).ToList();

            return db.Inventario
                     .Where(inv => !topViewedProductIds.Contains(inv.id_producto))
                     .GroupBy(inv => inv.id_producto)
                     .Select(g => g.OrderByDescending(inv => inv.veces_vendido).FirstOrDefault())
                     .OrderByDescending(inv => inv.veces_vendido)
                     .Take(8)
                     .ToList();
        }
    }
}