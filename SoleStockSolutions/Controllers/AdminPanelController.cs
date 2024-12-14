using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
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
                var productos = db.Productos
                    .Select(p => new ProductsDatatable
                    {
                        SKU = p.id_producto,
                        Marca = p.Marcas.nombre_marca,
                        Modelo = p.Modelos.nombre_modelo,
                        Nombre = p.nombre,
                        FechaLanzamiento = p.fecha_lanzamiento,
                        Imagen = p.imagen,
                        FechaUltimoUsoInterno = p.fecha_ultimo_uso_interno,
                        VecesVisto = p.veces_visto
                    }).ToList();
                ViewBag.ProductsDatatable = productos;
                ViewBag.Marcas = db.Marcas.Select(m => new { m.id_marca, m.nombre_marca }).ToList();
                ViewBag.Modelos = db.Modelos.Select(m => new { m.id_modelo, m.nombre_modelo }).ToList();
                ViewBag.CurrentAction = "AdminProducts";
                return View();
            }
        }

        [HttpPost]
        public JsonResult IsSkuUnique(string sku)
        {
            using (var db = new TFCEntities())
            {
                var isNotUnique = db.Productos.Any(p => p.id_producto == sku);
                return Json(isNotUnique, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult CreateProduct(Productos producto, HttpPostedFileBase imagen)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    producto.fecha_ultimo_uso_interno = DateTime.Now;

                    if (imagen != null && imagen.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(imagen.FileName);
                        var path = Path.Combine(Server.MapPath("~/Content/ProductImages"), fileName);
                        imagen.SaveAs(path);

                        producto.imagen = Url.Content(Path.Combine("~/Content/ProductImages", fileName));
                    }

                    db.Productos.Add(producto);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult UpdateProduct(Productos producto, HttpPostedFileBase imagen)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = db.Productos.Find(producto.id_producto);
                    if (existingProduct != null)
                    {
                        if (imagen != null && imagen.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(imagen.FileName);
                            var path = Path.Combine(Server.MapPath("~/Content/ProductImages"), fileName);
                            imagen.SaveAs(path);

                            producto.imagen = Url.Content(Path.Combine("~/Content/ProductImages", fileName));
                        }
                        else
                        {
                            producto.imagen = existingProduct.imagen;
                        }

                        db.Entry(existingProduct).CurrentValues.SetValues(producto);
                        existingProduct.fecha_ultimo_uso_interno = DateTime.Now;
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult DeleteProduct(string id)
        {
            using (var db = new TFCEntities())
            {
                var producto = db.Productos.Find(id);
                if (producto != null)
                {
                    db.Productos.Remove(producto);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public JsonResult GetProduct(string id)
        {
            using (var db = new TFCEntities())
            {
                if (string.IsNullOrEmpty(id))
                    return Json(null, JsonRequestBehavior.AllowGet);

                var product = db.Productos.FirstOrDefault(p => p.id_producto == id);
                if (product == null)
                    return Json(null, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    product.id_producto,
                    product.id_marca,
                    product.id_modelo,
                    product.nombre,
                    product.descripcion,
                    fecha_lanzamiento = product.fecha_lanzamiento?.ToString("yyyy-MM-dd"),
                    product.imagen
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SearchProducts(string searchTerm)
        {
            using (var db = new TFCEntities())
            {
                var products = db.Productos
                .Where(p => p.nombre.Contains(searchTerm) || p.id_producto.Contains(searchTerm) || p.Modelos.nombre_modelo.Contains(searchTerm) || p.Marcas.nombre_marca.Contains(searchTerm))
                .Select(p => new { p.id_producto, p.nombre, p.imagen })
                .Take(7)
                .ToList();

                return Json(products, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetModelosByMarca(int? marcaId)
        {
            using (var db = new TFCEntities())
            {
                var modelos = db.Modelos.Where(m => m.id_marca == marcaId).Select(m => new { m.id_modelo, m.nombre_modelo }).ToList();
                return Json(modelos, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetMarcaByModelo(int modeloId)
        {
            using (var db = new TFCEntities())
            {
                var marca = db.Modelos.Where(m => m.id_modelo == modeloId).Select(m => new { m.Marcas.id_marca, m.Marcas.nombre_marca }).FirstOrDefault();
                return Json(marca, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetAllModelos()
        {
            using (var db = new TFCEntities())
            {
                var modelos = db.Modelos.Select(m => new { m.id_modelo, m.nombre_modelo }).ToList();
                return Json(modelos, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Brands()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.Marcas = db.Marcas.ToList();
                ViewBag.CurrentAction = "AdminMarcas";
                return View();
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult CreateBrand(Marcas marca, HttpPostedFileBase logo_marca)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    if (logo_marca != null && logo_marca.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(logo_marca.FileName);
                        var path = Path.Combine(Server.MapPath("~/Content/BrandLogos"), fileName);
                        logo_marca.SaveAs(path);

                        marca.logo_marca = Url.Content(Path.Combine("~/Content/BrandLogos", fileName));
                    }

                    db.Marcas.Add(marca);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult UpdateBrand(Marcas marca, HttpPostedFileBase logo_marca)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    var existingMarca = db.Marcas.Find(marca.id_marca);
                    if (existingMarca != null)
                    {
                        if (logo_marca != null && logo_marca.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(logo_marca.FileName);
                            var path = Path.Combine(Server.MapPath("~/Content/BrandLogos"), fileName);
                            logo_marca.SaveAs(path);

                            marca.logo_marca = Url.Content(Path.Combine("~/Content/BrandLogos", fileName));
                        }
                        else
                        {
                            marca.logo_marca = existingMarca.logo_marca;
                        }
                        db.Entry(existingMarca).CurrentValues.SetValues(marca);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult DeleteBrand(int id)
        {
            using (var db = new TFCEntities())
            {
                var marca = db.Marcas.Find(id);
                if (marca != null)
                {
                    db.Marcas.Remove(marca);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        public JsonResult GetBrand(int id)
        {
            using (var db = new TFCEntities())
            {
                var marca = db.Marcas.FirstOrDefault(m => m.id_marca == id);
                if (marca == null)
                    return Json(null, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    marca.id_marca,
                    marca.nombre_marca,
                    marca.logo_marca
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult IsBrandNameUnique(string nombre_marca)
        {
            using (var db = new TFCEntities())
            {
                var isNotUnique = db.Marcas.Any(m => m.nombre_marca.Equals(nombre_marca, StringComparison.OrdinalIgnoreCase));
                return Json(isNotUnique, JsonRequestBehavior.AllowGet);
            }
        }
    }
}