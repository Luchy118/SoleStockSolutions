using Microsoft.Ajax.Utilities;
using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    public class ProductsController : Controller
    {
        private static int indexRedirCallsModelo = 0;
        private static int indexRedirCallsMarca = 0;

        public ActionResult Index(int? modeloId, int? marcaId)
        {
            if (modeloId.HasValue)
                Session["modeloId"] = modeloId;
            else if (marcaId.HasValue)
                Session["marcaId"] = marcaId;

            if (Session["modeloId"] != null)
            {
                modeloId = (int?)Session["modeloId"];
                indexRedirCallsModelo++;
                if (indexRedirCallsModelo == 2)
                {
                    indexRedirCallsModelo = 0;
                    Session["modeloId"] = null;
                }
            }
            else if (Session["marcaId"] != null)
            {
                marcaId = (int?)Session["marcaId"];
                indexRedirCallsMarca++;
                if (indexRedirCallsMarca == 2)
                {
                    indexRedirCallsMarca = 0;
                    Session["marcaId"] = null;
                }
            }

            using (var db = new TFCEntities())
            {
                var productosQuery = db.Productos.AsQueryable().Where(p => p.Inventario.Sum(i => i.cantidad) > 0);
                var inventariosQuery = db.Inventario.AsQueryable().Where(g => g.cantidad > 0);
                var marcasQuery = db.Marcas.AsQueryable();
                var modelosQuery = db.Modelos.AsQueryable();

                if (modeloId.HasValue)
                {
                    productosQuery = productosQuery.Where(p => p.id_modelo == modeloId.Value);
                    inventariosQuery = inventariosQuery.Where(i => productosQuery.Select(p => p.id_producto).Contains(i.id_producto));
                }
                else if (marcaId.HasValue)
                {
                    productosQuery = productosQuery.Where(p => p.id_marca == marcaId.Value);
                    inventariosQuery = inventariosQuery.Where(i => productosQuery.Select(p => p.id_producto).Contains(i.id_producto));
                    marcasQuery = marcasQuery.Where(m => m.id_marca == marcaId);
                    modelosQuery = modelosQuery.Where(m => m.Marcas.id_marca == marcaId);
                }

                var productos = productosQuery.ToList();
                var inventarios = inventariosQuery.ToList();
                var marcas = marcasQuery.ToList();
                var modelos = modelosQuery.ToList();

                var listaProductosDisponibles = inventarios
                    .GroupBy(i => i.id_producto)
                    .Select(g => g.FirstOrDefault())
                    .Where(g => g.cantidad > 0)
                    .ToList();

                ViewData["ListaProductosDisponibles"] = listaProductosDisponibles;
                ViewBag.CurrentAction = "Products";

                var marcasConCantidad = marcas.Select(m => new FiltroMarcas
                {
                    Id_Marca = m.id_marca,
                    Nombre_Marca = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.nombre_marca.ToLower()),
                    CantidadProductos = db.Productos
                        .Where(p => p.id_marca == m.id_marca)
                        .Join(db.Inventario, p => p.id_producto, i => i.id_producto, (p, i) => new { Producto = p, Inventario = i })
                        .Where(pi => pi.Inventario.cantidad > 0)
                        .Select(pi => pi.Producto)
                        .Distinct()
                        .Count()
                }).ToList();
                ViewBag.Marcas = marcasConCantidad;

                var modelosConCantidad = modelos.Select(m => new FiltroModelos
                {
                    Id_Modelo = m.id_modelo,
                    Nombre_Modelo = m.nombre_modelo,
                    CantidadProductos = db.Productos
                        .Where(p => p.id_modelo == m.id_modelo)
                        .Join(db.Inventario, p => p.id_producto, i => i.id_producto, (p, i) => new { p, i })
                        .Where(pi => pi.i.cantidad > 0)
                        .Select(pi => pi.p)
                        .Distinct()
                        .Count()
                }).ToList();
                ViewBag.Modelos = modelosConCantidad;

                var tallas = inventarios
                    .Select(i => new { i.talla_eu, i.talla_eu_marca })
                    .ToList()
                    .Select(i => i.talla_eu ?? i.talla_eu_marca)
                    .GroupBy(t => t)
                    .Select(g => new FiltroTallas
                    {
                        Talla = g.Key,
                        CantidadProductos = g.Count()
                    })
                    .ToList();
                var tallasOrdenadas = tallas.Select(t =>
                {
                    double valorNumerico;
                    string tallaOriginal = t.Talla;

                    if (tallaOriginal.Contains("1/3"))
                        valorNumerico = double.Parse(tallaOriginal.Replace(" 1/3", "").Replace(",", ".")) + 0.3;
                    else if (tallaOriginal.Contains("2/3"))
                        valorNumerico = double.Parse(tallaOriginal.Replace(" 2/3", "").Replace(",", ".")) + 0.6;
                    else
                        valorNumerico = double.Parse(tallaOriginal.Replace(".", ","));

                    return new { Original = tallaOriginal, Numerico = valorNumerico };
                })
                .OrderBy(t => t.Numerico)
                .ToList();
                var result = tallasOrdenadas.Select(t => new FiltroTallas
                {
                    Talla = t.Original,
                    CantidadProductos = tallas.First(s => s.Talla == t.Original).CantidadProductos
                });
                ViewBag.Tallas = result;

                var minPrice = listaProductosDisponibles.Min(p => p.precio);
                var maxPrice = listaProductosDisponibles.Max(p => p.precio);
                var priceRange = new FiltroPrecios
                {
                    MinPrice = minPrice,
                    MaxPrice = maxPrice
                };
                ViewBag.PriceRange = priceRange;
                ViewBag.InitialMin = priceRange.MinPrice;
                ViewBag.InitialMax = priceRange.MaxPrice;
                ViewBag.SelectedModel = modeloId;
                ViewBag.SelectedBrands = new List<int?>() { marcaId };

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

        public ActionResult ProductDetails(string productName)
        {
            if (productName == "index")
                return null;

            if (string.IsNullOrEmpty(productName))
                throw new HttpException(404, "Página no encontrada");

            using (var db = new TFCEntities())
            {
                var producto = db.Productos.FirstOrDefault(p => p.nombre.ToLower().Replace(" ", "-").Replace("(", "").Replace(")", "") == productName);

                if (producto.Inventario.Sum(i => i.cantidad) == 0)
                    throw new HttpException(404, "Página no encontrada");

                if (producto != null)
                {
                    producto.veces_visto += 1;
                    db.SaveChanges();
                }

                ViewBag.TotalCantidad = producto.Inventario.Sum(i => i.cantidad);
                ViewBag.TotalVendidos = producto.Inventario.Sum(i => i.veces_vendido);

                var tallas = producto.Inventario.Where(i => i.cantidad > 0).Select(i => i.talla_eu ?? i.talla_eu_marca).ToList();
                var tallasOrdenadas = tallas.Select(t =>
                {
                    double valorNumerico;
                    string tallaOriginal = t;

                    if (tallaOriginal.Contains("1/3"))
                        valorNumerico = double.Parse(tallaOriginal.Replace(" 1/3", "").Replace(",", ".")) + 0.3;
                    else if (tallaOriginal.Contains("2/3"))
                        valorNumerico = double.Parse(tallaOriginal.Replace(" 2/3", "").Replace(",", ".")) + 0.6;
                    else
                        valorNumerico = double.Parse(tallaOriginal.Replace(".", ","));

                    return new { Original = tallaOriginal, Numerico = valorNumerico };
                })
                .OrderBy(t => t.Numerico)
                .Select(t => t.Original)
                .ToList();
                ViewBag.TallasOrdenadas = tallasOrdenadas;

                var relatedProducts = db.Productos
                    .Where(p => p.Marcas.id_marca == producto.Marcas.id_marca
                                && p.Modelos.id_modelo == producto.Modelos.id_modelo
                                && p.id_producto != producto.id_producto
                                && p.Inventario.Any(i => i.cantidad > 0))
                    .Select(p => new RelatedProducts
                    {
                        Producto = p,
                        PrecioMinimo = p.Inventario.Where(i => i.cantidad > 0).Min(i => (int?)i.precio) ?? 0
                    })
                    .Take(10)
                    .ToList();
                if (relatedProducts.Count < 10)
                {
                    var additionalProducts = db.Productos
                        .Where(p => p.Marcas.id_marca == producto.Marcas.id_marca
                                    && p.Modelos.id_modelo != producto.Modelos.id_modelo
                                    && p.id_producto != producto.id_producto
                                    && p.Inventario.Any(i => i.cantidad > 0))
                        .Select(p => new RelatedProducts
                        {
                            Producto = p,
                            PrecioMinimo = p.Inventario.Where(i => i.cantidad > 0).Min(i => (int?)i.precio) ?? 0
                        })
                        .OrderBy(r => Guid.NewGuid())
                        .Take(10 - relatedProducts.Count)
                        .ToList();

                    relatedProducts.AddRange(additionalProducts);
                }
                ViewBag.RelatedProducts = relatedProducts;

                if (User.Identity.IsAuthenticated)
                {
                    var userId = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name)?.id_usuario;
                    var wishlistProducts = db.Wishlist
                        .Where(w => w.id_usuario == userId)
                        .Select(w => w.Productos.id_producto)
                        .ToList();

                    ViewBag.WishlistProductsIds = wishlistProducts;
                }

                return View(producto);
            }
        }

        public PartialViewResult GetBrandFilter(List<int?> selectedBrands, int? modelId, List<string> selectedSizes = null)
        {
            using (var db = new TFCEntities())
            {
                var brands = db.Marcas.AsQueryable();

                if (selectedSizes != null && selectedSizes.Any())
                {
                    var brandIds = db.Inventario
                        .Where(i => selectedSizes.Contains(i.talla_eu) || selectedSizes.Contains(i.talla_eu_marca))
                        .Select(i => i.id_marca)
                        .Distinct()
                        .ToList();

                    brands = brands.Where(m => brandIds.Contains(m.id_marca));
                }

                if (modelId.HasValue)
                {
                    var brandIds = db.Productos
                        .Where(p => p.id_modelo == modelId.Value)
                        .Select(p => p.id_marca)
                        .Distinct()
                        .ToList();

                    brands = brands.Where(m => brandIds.Contains(m.id_marca));
                }

                var productCounts = db.Productos
                    .Join(db.Inventario.Where(i => i.cantidad > 0), p => p.id_producto, i => i.id_producto, (p, i) => new { p.id_marca, p.id_producto })
                    .GroupBy(p => p.id_marca)
                    .Select(g => new { Id_Marca = g.Key, CantidadProductos = g.Select(p => p.id_producto).Distinct().Count() })
                    .ToDictionary(x => x.Id_Marca, x => x.CantidadProductos);

                var result = brands
                    .ToList()
                    .Select(m => new FiltroMarcas
                    {
                        Id_Marca = m.id_marca,
                        Nombre_Marca = m.nombre_marca,
                        CantidadProductos = productCounts.ContainsKey(m.id_marca) ? productCounts[m.id_marca] : 0
                    })
                    .Where(m => m.CantidadProductos > 0)
                    .OrderBy(m => m.Nombre_Marca)
                    .ToList();

                ViewBag.SelectedBrands = selectedBrands;
                ViewBag.SelectedModel = modelId;
                ViewBag.SelectedSizes = selectedSizes;

                return PartialView("_BrandFilter", result);
            }
        }

        public PartialViewResult GetModelFilter(List<int?> selectedBrands, int? modelId, List<string> selectedSizes = null)
        {
            using (var db = new TFCEntities())
            {
                var modelos = db.Modelos.AsQueryable();

                if (selectedBrands != null && selectedBrands.Any())
                    modelos = modelos.Where(m => selectedBrands.Contains(m.id_marca));

                if (selectedSizes != null && selectedSizes.Any())
                {
                    var productIds = db.Inventario
                        .Where(i => selectedSizes.Contains(i.talla_eu) || selectedSizes.Contains(i.talla_eu_marca))
                        .Select(i => i.id_producto)
                        .Distinct()
                        .ToList();

                    modelos = modelos.Where(m => db.Productos
                        .Where(p => productIds.Contains(p.id_producto))
                        .Select(p => p.id_modelo)
                        .Contains(m.id_modelo));
                }

                var result = modelos
                    .Select(m => new FiltroModelos
                    {
                        Id_Modelo = m.id_modelo,
                        Nombre_Modelo = m.nombre_modelo,
                        CantidadProductos = db.Productos
                            .Where(p => p.id_modelo == m.id_modelo)
                            .Join(db.Inventario, p => p.id_producto, i => i.id_producto, (p, i) => new { p, i })
                            .Where(pi => pi.i.cantidad > 0)
                            .Select(pi => pi.p)
                            .Distinct()
                            .Count()
                    })
                    .Where(m => m.CantidadProductos > 0)
                    .ToList();

                ViewBag.SelectedBrands = selectedBrands;
                ViewBag.SelectedModel = modelId;
                ViewBag.SelectedSizes = selectedSizes;

                return PartialView("_ModelFilter", result);
            }
        }

        public ActionResult GetSizeFilter(List<int?> selectedBrands, int? modelId, List<string> selectedSizes = null)
        {
            using (var db = new TFCEntities())
            {
                var products = db.Inventario.AsQueryable().Where(g => g.cantidad > 0);

                if (selectedBrands != null && selectedBrands.Any())
                    products = products.Where(p => selectedBrands.Contains(p.id_marca));

                if (modelId.HasValue)
                {
                    var productIds = db.Productos.Where(p => p.id_modelo == modelId.Value).Select(p => p.id_producto).ToList();
                    products = products.Where(p => productIds.Contains(p.id_producto));
                }

                var sizes = products
                    .Select(i => new { i.talla_eu, i.talla_eu_marca })
                    .ToList()
                    .Select(i => i.talla_eu ?? i.talla_eu_marca)
                    .GroupBy(t => t)
                    .Select(g => new
                    {
                        talla = g.Key,
                        cantidad = g.Count()
                    })
                    .ToList();

                var orderedSizes = sizes.Select(t =>
                {
                    double valorNumerico;
                    string tallaOriginal = t.talla;

                    if (tallaOriginal.Contains("1/3"))
                        valorNumerico = double.Parse(tallaOriginal.Replace(" 1/3", "").Replace(",", ".")) + 0.3;
                    else if (tallaOriginal.Contains("2/3"))
                        valorNumerico = double.Parse(tallaOriginal.Replace(" 2/3", "").Replace(",", ".")) + 0.6;
                    else
                        valorNumerico = double.Parse(tallaOriginal.Replace(".", ","));

                    return new { Original = tallaOriginal, Numerico = valorNumerico };
                })
                .OrderBy(t => t.Numerico)
                .ToList();

                var result = orderedSizes.Select(t => new FiltroTallas
                {
                    Talla = t.Original,
                    CantidadProductos = sizes.First(s => s.talla == t.Original).cantidad
                }).ToList();

                ViewBag.SelectedBrands = selectedBrands;
                ViewBag.SelectedModel = modelId;
                ViewBag.SelectedSizes = selectedSizes;

                return PartialView("_SizeFilter", result);
            }
        }

        public JsonResult GetPriceRange(List<int?> selectedBrands, int? modelId, List<string> selectedSizes = null)
        {
            using (var db = new TFCEntities())
            {
                var query = db.Inventario.AsQueryable().Where(g => g.cantidad > 0);

                if (selectedBrands != null && selectedBrands.Any())
                    query = query.Where(i => selectedBrands.Contains(i.id_marca));

                if (selectedSizes != null && selectedSizes.Any())
                    query = query.Where(i => selectedSizes.Contains(i.talla_eu) || selectedSizes.Contains(i.talla_eu_marca));

                if (modelId.HasValue)
                    query = query.Where(i => i.Productos.id_modelo == modelId.Value);

                var prices = query.Select(i => i.precio).ToList();
                var rangeMinPrice = prices.Min();
                var rangeMaxPrice = prices.Max();

                return Json(new { rangeMinPrice, rangeMaxPrice }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult FilterProducts(string query, List<int?> selectedBrands, int? modelId, List<string> selectedSizes = null, string sortOrder = null)
        {
            using (var db = new TFCEntities())
            {
                var products = db.Inventario.AsQueryable().Where(g => g.cantidad > 0);

                if (selectedBrands != null && selectedBrands.Any())
                    products = products.Where(p => selectedBrands.Contains(p.id_marca));

                if (selectedSizes != null && selectedSizes.Any())
                    products = products.Where(p => selectedSizes.Contains(p.talla_eu) || selectedSizes.Contains(p.talla_eu_marca));

                if (modelId.HasValue)
                {
                    var productIds = db.Productos.Where(p => p.id_modelo == modelId.Value).Select(p => p.id_producto).ToList();
                    products = products.Where(p => productIds.Contains(p.id_producto));
                }

                if (!string.IsNullOrEmpty(query))
                    products = products.Where(p => p.Productos.nombre.Contains(query) || p.Productos.Modelos.nombre_modelo.Contains(query) || p.Productos.Marcas.nombre_marca.Contains(query));

                var filteredProducts = products
                    .GroupBy(p => p.id_producto)
                    .Select(g => g.FirstOrDefault())
                    .ToList();

                if (!string.IsNullOrEmpty(sortOrder))
                {
                    switch (sortOrder)
                    {
                        case "relevancia":
                            filteredProducts = filteredProducts.OrderByDescending(p => p.veces_vendido + p.Productos.veces_visto).ToList();
                            break;
                        case "mas_vendidos":
                            filteredProducts = filteredProducts.OrderByDescending(p => p.veces_vendido).ToList();
                            break;
                        case "mas_vistos":
                            filteredProducts = filteredProducts.OrderByDescending(p => p.Productos.veces_visto).ToList();
                            break;
                        case "precio_mayor_a_menor":
                            filteredProducts = filteredProducts.OrderByDescending(p => p.precio).ToList();
                            break;
                        case "precio_menor_a_mayor":
                            filteredProducts = filteredProducts.OrderBy(p => p.precio).ToList();
                            break;
                        case "titulo_a_z":
                            filteredProducts = filteredProducts.OrderBy(p => p.Productos.nombre).ToList();
                            break;
                        case "titulo_z_a":
                            filteredProducts = filteredProducts.OrderByDescending(p => p.Productos.nombre).ToList();
                            break;
                        case "mas_nuevos":
                            filteredProducts = filteredProducts.OrderByDescending(p => p.Productos.fecha_lanzamiento).ToList();
                            break;
                    }
                }

                if (User.Identity.IsAuthenticated)
                {
                    var userId = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name)?.id_usuario;
                    var wishlistProducts = db.Wishlist
                        .Where(w => w.id_usuario == userId)
                        .Select(w => w.Productos.id_producto)
                        .ToList();

                    ViewBag.WishlistProductsIds = wishlistProducts;
                }

                var productListHtml = RenderPartialViewToString("_ProductList", filteredProducts);
                var productCount = filteredProducts.Count;

                return Json(new { productListHtml, productCount }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult SortProducts(string query, string sortOrder, List<int?> selectedBrands = null, int? modelId = null, List<string> selectedSizes = null)
        {
            using (var db = new TFCEntities())
            {
                var products = db.Inventario.AsQueryable();

                if (selectedBrands != null && selectedBrands.Any())
                    products = products.Where(p => selectedBrands.Contains(p.id_marca));

                if (selectedSizes != null && selectedSizes.Any())
                    products = products.Where(p => selectedSizes.Contains(p.talla_eu) || selectedSizes.Contains(p.talla_eu_marca));

                if (modelId.HasValue)
                {
                    var productIds = db.Productos.Where(p => p.id_modelo == modelId.Value).Select(p => p.id_producto).ToList();
                    products = products.Where(p => productIds.Contains(p.id_producto));
                }

                if (!string.IsNullOrEmpty(query))
                    products = products.Where(p => p.Productos.nombre.Contains(query) || p.Productos.Modelos.nombre_modelo.Contains(query) || p.Productos.Marcas.nombre_marca.Contains(query));

                var filteredProducts = products.GroupBy(p => p.id_producto).Select(g => g.FirstOrDefault()).Where(g => g.cantidad > 0);

                switch (sortOrder)
                {
                    case "relevancia":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.veces_vendido + p.Productos.veces_visto);
                        break;
                    case "mas_vendidos":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.veces_vendido);
                        break;
                    case "mas_vistos":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Productos.veces_visto);
                        break;
                    case "precio_mayor_a_menor":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.precio);
                        break;
                    case "precio_menor_a_mayor":
                        filteredProducts = filteredProducts.OrderBy(p => p.precio);
                        break;
                    case "titulo_a_z":
                        filteredProducts = filteredProducts.OrderBy(p => p.Productos.nombre);
                        break;
                    case "titulo_z_a":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Productos.nombre);
                        break;
                    case "mas_nuevos":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Productos.fecha_lanzamiento);
                        break;
                }

                if (User.Identity.IsAuthenticated)
                {
                    var userId = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name)?.id_usuario;
                    var wishlistProducts = db.Wishlist
                        .Where(w => w.id_usuario == userId)
                        .Select(w => w.Productos.id_producto)
                        .ToList();

                    ViewBag.WishlistProductsIds = wishlistProducts;
                }

                var productListHtml = RenderPartialViewToString("_ProductList", filteredProducts.ToList());
                return Json(new { productListHtml }, JsonRequestBehavior.AllowGet);
            }
        }

        private string RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}