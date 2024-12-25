using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RestSharp;
using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    [CustomAuthorize(Roles = "Admin")]
    public class AdminPanelController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.CurrentAction = "AdminIndex";
                return View();
            }
        }

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
                var searchTerms = searchTerm.ToLower().Split(' ');
                var products = db.Productos
                    .Where(p => searchTerms.All(term => p.nombre.ToLower().Contains(term) || p.id_producto.ToLower().Contains(term) || p.Modelos.nombre_modelo.ToLower().Contains(term) || p.Marcas.nombre_marca.ToLower().Contains(term)))
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

        [HttpPost]
        public JsonResult IsSkuUnique(string sku)
        {
            using (var db = new TFCEntities())
            {
                var isNotUnique = db.Productos.Any(p => p.id_producto == sku);
                return Json(isNotUnique, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Brands()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.Marcas = db.Marcas.ToList();
                ViewBag.CurrentAction = "AdminBrands";
                return View();
            }
        }

        [HttpPost]
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
        public ActionResult SearchMarcas(string searchTerm)
        {
            using (var db = new TFCEntities())
            {
                var marcas = db.Marcas
                    .Where(m => m.nombre_marca.Contains(searchTerm))
                    .Select(m => new { m.id_marca, m.nombre_marca, m.logo_marca })
                    .Take(7)
                    .ToList();

                return Json(marcas, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
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

        public ActionResult Models()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.Modelos = db.Modelos
                    .Select(m => new ModelsDatatable
                    {
                        Id_Modelo = m.id_modelo,
                        Nombre_Modelo = m.nombre_modelo,
                        Id_Marca = m.id_marca,
                        Nombre_Marca = m.Marcas.nombre_marca
                    }).ToList();
                ViewBag.CurrentAction = "AdminModels";
                return View();
            }
        }

        [HttpPost]
        public ActionResult CreateModel(Modelos modelo)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    db.Modelos.Add(modelo);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult UpdateModel(Modelos modelo)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    var existingModel = db.Modelos.Find(modelo.id_modelo);
                    if (existingModel != null)
                    {
                        db.Entry(existingModel).CurrentValues.SetValues(modelo);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult DeleteModel(int id)
        {
            using (var db = new TFCEntities())
            {
                var modelo = db.Modelos.Find(id);
                if (modelo != null)
                {
                    db.Modelos.Remove(modelo);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SearchModelos(string searchTerm)
        {
            using (var db = new TFCEntities())
            {
                var modelos = db.Modelos
                    .Where(m => m.nombre_modelo.Contains(searchTerm))
                    .Select(m => new { m.id_modelo, m.nombre_modelo })
                    .Take(7)
                    .ToList();

                return Json(modelos, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GetModelData(int id)
        {
            using (var db = new TFCEntities())
            {
                var modelo = db.Modelos
                    .Where(m => m.id_modelo == id)
                    .Select(m => new ModelsDatatable
                    {
                        Id_Modelo = m.id_modelo,
                        Nombre_Modelo = m.nombre_modelo,
                        Id_Marca = m.id_marca,
                        Nombre_Marca = m.Marcas.nombre_marca
                    })
                    .FirstOrDefault();

                if (modelo == null)
                    return Json(null, JsonRequestBehavior.AllowGet);

                return Json(modelo, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult IsModelNameUnique(string nombre_modelo)
        {
            using (var db = new TFCEntities())
            {
                var isNotUnique = db.Modelos.Any(m => m.nombre_modelo.Equals(nombre_modelo, StringComparison.OrdinalIgnoreCase));
                return Json(isNotUnique, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DoesBrandExist(string nombre_marca)
        {
            using (var db = new TFCEntities())
            {
                var marca = db.Marcas.FirstOrDefault(m => m.nombre_marca == nombre_marca);
                if (marca != null)
                    return Json(new { exists = true, marca.id_marca }, JsonRequestBehavior.AllowGet);
                else
                    return Json(new { exists = false }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult Sizes()
        {
            using (var db = new TFCEntities())
            {
                var model = new SizesViewModel
                {
                    UniversalSizes = db.Tallas_Universales.ToList(),
                    BrandSizes = db.Tallas_Marcas
                        .Select(tm => new BrandSizesDatatable
                        {
                            Talla_Eu = tm.talla_eu,
                            Id_Marca = tm.id_marca,
                            Nombre_Marca = tm.Marcas.nombre_marca
                        })
                        .ToList(),
                };
                ViewBag.CurrentAction = "AdminSizes";
                return View(model);
            }
        }

        [HttpPost]
        public ActionResult CreateUniversalSize(Tallas_Universales talla)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    if (!db.Tallas_Universales.Any(t => t.talla_eu == talla.talla_eu))
                    {
                        db.Tallas_Universales.Add(talla);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false, error = "La talla ya existe." }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult UpdateUniversalSize(Tallas_Universales talla)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    var existingSize = db.Tallas_Universales.Find(talla.talla_eu);
                    if (existingSize != null)
                    {
                        db.Entry(existingSize).CurrentValues.SetValues(talla);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult DeleteUniversalSize(string talla_eu)
        {
            using (var db = new TFCEntities())
            {
                var talla = db.Tallas_Universales.Find(talla_eu);
                if (talla != null)
                {
                    db.Tallas_Universales.Remove(talla);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult CreateBrandSize(Tallas_Marcas talla)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    if (!db.Tallas_Marcas.Any(t => t.talla_eu == talla.talla_eu && t.id_marca == talla.id_marca))
                    {
                        db.Tallas_Marcas.Add(talla);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false, error = "La talla ya existe para esta marca." }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult UpdateBrandSize(Tallas_Marcas talla)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    var existingSize = db.Tallas_Marcas.Find(talla.id_marca, talla.talla_eu);
                    if (existingSize != null)
                    {
                        db.Entry(existingSize).CurrentValues.SetValues(talla);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult DeleteBrandSize(int id_marca, string talla_eu)
        {
            using (var db = new TFCEntities())
            {
                var talla = db.Tallas_Marcas.Find(id_marca, talla_eu);
                if (talla != null)
                {
                    db.Tallas_Marcas.Remove(talla);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SearchUniversalSizes(string searchTerm)
        {
            using (var db = new TFCEntities())
            {
                var sizes = db.Tallas_Universales
               .Where(s => s.talla_eu.Contains(searchTerm))
               .Select(s => new { s.talla_eu })
               .ToList();

                return Json(sizes);
            }
        }

        [HttpPost]
        public JsonResult SearchBrandSizes(int idMarca, string searchTerm)
        {
            using (var db = new TFCEntities())
            {
                var tallas = db.Tallas_Marcas
                .Where(tm => tm.id_marca == idMarca && tm.talla_eu.Contains(searchTerm))
                .Select(tm => tm.talla_eu)
                .ToList();

                return Json(tallas);
            }
        }


        [HttpPost]
        public ActionResult GetUniversalSize(string id)
        {
            using (var db = new TFCEntities())
            {
                var size = db.Tallas_Universales
                .Where(s => s.talla_eu == id)
                .Select(s => new { s.talla_eu, s.talla_us, s.talla_uk })
                .FirstOrDefault();

                return Json(size);
            }
        }

        [HttpPost]
        public ActionResult GetBrandSize(string id)
        {
            using (var db = new TFCEntities())
            {
                var size = db.Tallas_Marcas
                .Where(s => s.talla_eu == id)
                .Select(s => new { s.id_marca, s.Marcas.nombre_marca, s.talla_eu, s.talla_us, s.talla_uk })
                .FirstOrDefault();

                return Json(size);
            }
        }

        [HttpPost]
        public JsonResult SearchBrands(string searchTerm)
        {
            using (var db = new TFCEntities())
            {
                var marcasConTallas = db.Tallas_Marcas
                .Where(tm => tm.Marcas.nombre_marca.Contains(searchTerm))
                .Select(tm => new { tm.Marcas.nombre_marca, tm.id_marca, tm.Marcas.logo_marca })
                .Distinct()
                .ToList();

                return Json(marcasConTallas, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DoesSizeExist(string talla_eu, int? id_marca)
        {
            using (var db = new TFCEntities())
            {
                if (id_marca.HasValue)
                {
                    var tallaMarca = db.Tallas_Marcas.FirstOrDefault(t => t.talla_eu == talla_eu && t.id_marca == id_marca.Value);
                    if (tallaMarca != null)
                        return Json(new { exists = true, tallaMarca.talla_us, tallaMarca.talla_uk });
                }
                else
                {
                    var tallaUniversal = db.Tallas_Universales.FirstOrDefault(t => t.talla_eu == talla_eu);
                    if (tallaUniversal != null)
                        return Json(new { exists = true, tallaUniversal.talla_us, tallaUniversal.talla_uk });
                }

                return Json(new { exists = false });
            }
        }

        public ActionResult Colors()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.Colores = db.Colores.ToList();
                ViewBag.CurrentAction = "AdminColors";
                return View();
            }
        }

        [HttpPost]
        public JsonResult SearchColores(string searchTerm)
        {
            using (var db = new TFCEntities())
            {
                var colores = db.Colores
                .Where(c => c.nombre_color.Contains(searchTerm))
                .Select(c => new { c.hex, c.nombre_color })
                .ToList();

                return Json(colores, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetColor(string hex)
        {
            using (var db = new TFCEntities())
            {
                var color = db.Colores
                .Where(c => c.hex == hex)
                .Select(c => new { c.hex, c.nombre_color })
                .FirstOrDefault();

                return Json(color, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CreateColor(Colores color)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    if (!db.Colores.Any(c => c.hex == color.hex))
                    {
                        db.Colores.Add(color);
                        db.SaveChanges();
                        return Json(new { success = true });
                    }
                    return Json(new { success = false, error = "El color ya existe" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, error = "Error al crear el color" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult UpdateColor(Colores color)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    var existingColor = db.Colores.FirstOrDefault(c => c.nombre_color == color.nombre_color);
                    if (existingColor != null)
                    {
                        db.Colores.Remove(existingColor);
                        db.Colores.Add(color);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                }

                return Json(new { success = false, error = "Error al actualizar el color" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteColor(string hex)
        {
            using (var db = new TFCEntities())
            {
                var color = db.Colores.Find(hex);
                if (color != null)
                {
                    db.Colores.Remove(color);
                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = false, error = "Error al eliminar el color" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CheckDuplicateColorName(string nombre_color)
        {
            using (var db = new TFCEntities())
            {
                bool isDuplicate = db.Colores.Any(c => c.nombre_color == nombre_color);
                return Json(new { exists = isDuplicate }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Inventory()
        {
            using (var db = new TFCEntities())
            {
                var inventoryList = db.Inventario.Include(i => i.Productos).Include(i => i.Tallas_Universales).Include(i => i.Tallas_Marcas).ToList();
                var groupedInventory = inventoryList
                    .Where(i => i.Productos.id_marca != null)
                    .GroupBy(i => i.id_producto)
                    .Select(g => new InventoryDatatable
                    {
                        SKU = g.Key,
                        Image = g.First().Productos.imagen,
                        ProductName = g.First().Productos.nombre,
                        Brand = g.First().Productos.Marcas.nombre_marca,
                        TotalStock = g.Sum(i => i.cantidad),
                        MarketValue = g.Sum(i => i.cantidad * i.precio),
                        TotalSales = g.Sum(i => i.veces_vendido),
                        LastModified = g.Max(i => i.fecha_actualizacion)
                    }).ToList();

                ViewBag.Inventory = groupedInventory;
                ViewBag.CurrentAction = "AdminInventory";
                return View();
            }
        }

        [HttpGet]
        public ActionResult GetInventoryItemDetails(string sku)
        {
            using (var db = new TFCEntities())
            {
                var product = db.Productos.FirstOrDefault(p => p.id_producto == sku);
                if (product == null)
                    return HttpNotFound();

                var inventoryDetails = new
                {
                    SKU = product.id_producto,
                    ProductName = product.nombre,
                    Image = product.imagen,
                    Details = db.Inventario
                        .Where(d => d.id_producto == sku)
                        .Select(d => new
                        {
                            Size = d.talla_eu ?? d.talla_eu_marca,
                            Stock = d.cantidad,
                            Price = d.precio,
                            SalesNo = d.veces_vendido,
                            LastModified = d.fecha_actualizacion
                        })
                        .ToList()
                };

                return Json(inventoryDetails, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult UpdateInventoryItem(string productId, string size, int stock, int price)
        {
            using (var db = new TFCEntities())
            {
                var line = db.Inventario.FirstOrDefault(i => i.id_producto == productId && (i.talla_eu == size || i.talla_eu_marca == size));
                if (line != null)
                {
                    line.cantidad = stock;
                    line.precio = price;
                    line.fecha_actualizacion = DateTime.Now;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Error al actualizar la línea." });
            }
        }

        [HttpPost]
        public JsonResult AddStock(string productId, string size, int stock, int price)
        {
            try
            {
                using (var db = new TFCEntities())
                {
                    var existingItem = db.Inventario.FirstOrDefault(i => i.id_producto == productId && (i.talla_eu == size || i.talla_eu_marca == size));
                    if (existingItem != null)
                    {
                        existingItem.cantidad += stock;
                        existingItem.fecha_actualizacion = DateTime.Now;
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }

                    var validSize = db.Tallas_Universales.Any(t => t.talla_eu == size) || db.Tallas_Marcas.Any(t => t.talla_eu == size);
                    if (!validSize)
                        return Json(new { success = false, error = "Talla no válida." }, JsonRequestBehavior.AllowGet);

                    var product = db.Productos.FirstOrDefault(p => p.id_producto == productId);
                    var newItem = new Inventario
                    {
                        id_producto = productId,
                        talla_eu = product.id_marca.HasValue ? null : size,
                        talla_eu_marca = product.id_marca.HasValue ? size : null,
                        cantidad = stock,
                        precio = price,
                        fecha_actualizacion = DateTime.Now
                    };
                    db.Inventario.Add(newItem);
                    db.SaveChanges();

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteInventoryItem(string productId)
        {
            try
            {
                using (var db = new TFCEntities())
                {
                    var lines = db.Inventario.Where(p => p.id_producto == productId).ToList();

                    foreach(var line in lines)
                        db.Inventario.Remove(line);

                    db.SaveChanges();

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteInventoryItemSize(string productId, string size)
        {
            try
            {
                using (var db = new TFCEntities())
                {
                    var line = db.Inventario.FirstOrDefault(p => p.id_producto == productId && (p.talla_eu == size || p.talla_eu_marca == size));

                    db.Inventario.Remove(line);
                    db.SaveChanges();

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult AddInventoryItem(string productId, List<SizeStockPrice> sizes)
        {
            try
            {
                using (var db = new TFCEntities())
                {
                    var existingProduct = db.Productos.FirstOrDefault(p => p.id_producto == productId);

                    foreach (var size in sizes)
                    {
                        var tallaMarca = db.Tallas_Marcas.FirstOrDefault(tm => tm.id_marca == existingProduct.id_marca && tm.talla_eu == size.Size);

                        var inventario = new Inventario
                        {
                            id_producto = productId,
                            id_marca = existingProduct.id_marca,
                            cantidad = size.Stock,
                            precio = size.Price,
                            fecha_actualizacion = DateTime.Now,
                            talla_eu = tallaMarca == null ? size.Size : null,
                            talla_eu_marca = tallaMarca != null ? size.Size : null
                        };

                        db.Inventario.Add(inventario);
                    }

                    db.SaveChanges();
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult IsProductInInventory(string productId)
        {
            using (var db = new TFCEntities())
            {
                var exists = db.Inventario.Any(item => item.id_producto == productId);
                return Json(new { exists }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ValidateSize(string size, string productId)
        {
            using (var db = new TFCEntities())
            {
                var product = db.Productos.FirstOrDefault(p => p.id_producto == productId);
                var productUsesBrandSizes = db.Tallas_Marcas.Any(tm => tm.id_marca == product.id_marca);
                bool isValid = false;

                if (productUsesBrandSizes)
                    isValid = db.Tallas_Marcas.Any(tm => tm.id_marca == product.id_marca && tm.talla_eu == size);

                if (!isValid)
                    isValid = db.Tallas_Universales.Any(tu => tu.talla_eu == size);

                return Json(new { isValid }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<ActionResult> ImportExcel(HttpPostedFileBase excelFile)
        {
            using (var db = new TFCEntities())
            {
                if (excelFile == null || excelFile.ContentLength == 0)
                    return Json(new { success = false, message = "No se ha seleccionado ningún archivo." }, JsonRequestBehavior.AllowGet);

                var invalidEntries = new List<string>();
                var inventoryUpdates = new Dictionary<string, (int stock, int price)>();

                using (var package = new ExcelPackage(excelFile.InputStream))
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        return Json(new { success = false, message = "El archivo no contiene ninguna hoja." }, JsonRequestBehavior.AllowGet);

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var sku = worksheet.Cells[row, 1].Text.Trim();
                        var size = worksheet.Cells[row, 2].Text.Trim();
                        var stockText = worksheet.Cells[row, 3].Text.Trim();
                        var priceText = worksheet.Cells[row, 4].Text.Trim();

                        if (string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(size) || string.IsNullOrEmpty(stockText) || string.IsNullOrEmpty(priceText))
                        {
                            if (!string.IsNullOrEmpty(sku) || !string.IsNullOrEmpty(size) || !string.IsNullOrEmpty(stockText) || !string.IsNullOrEmpty(priceText))
                                invalidEntries.Add($"Fila {row}, Una o más columnas están vacías.");
                            continue;
                        }

                        if (!int.TryParse(stockText, out int stock))
                        {
                            invalidEntries.Add($"Fila {row}, Stock no válido para SKU: {sku}, Talla: {size}.");
                            continue;
                        }

                        if (!int.TryParse(priceText, out int price))
                        {
                            invalidEntries.Add($"Fila {row}, Precio no válido para SKU: {sku}, Talla: {size}.");
                            continue;
                        }

                        var key = $"{sku}/{size}";
                        if (inventoryUpdates.ContainsKey(key))
                            inventoryUpdates[key] = (inventoryUpdates[key].stock + stock, price);
                        else
                            inventoryUpdates[key] = (stock, price);

                        var product = db.Productos.FirstOrDefault(p => p.id_producto == sku);
                        if (product == null)
                        {
                            JObject searchResult = await SearchProduct(sku);
                            if (searchResult == null)
                            {
                                invalidEntries.Add($"Fila {row}, SKU no encontrado: {sku}.");
                                continue;
                            }
                            searchResult = (JObject)searchResult["hits"].First;
                            var marca = searchResult["brand"].ToString().ToLower();
                            var idMarca = db.Marcas.FirstOrDefault(m => m.nombre_marca.ToLower() == marca).id_marca;

                            product = new Productos
                            {
                                id_producto = sku,
                                nombre = searchResult["title"].ToString(),
                                id_marca = idMarca,
                                id_modelo = AsignarIdModelo(searchResult["title"].ToString(), idMarca),
                                imagen = searchResult["image"].ToString(),
                                fecha_lanzamiento = Convert.ToDateTime(searchResult["releaseDate"]),
                                fecha_ultimo_uso_interno = DateTime.Now,
                                precio_medio_mercado = await ObtenerPrecioMedioMercado(Convert.ToDecimal(searchResult["avg_price"]))
                            };
                            db.Productos.Add(product);
                            db.SaveChanges();
                        }

                        var universalSize = db.Tallas_Universales.FirstOrDefault(t => t.talla_eu == size);
                        var brandSize = db.Tallas_Marcas.FirstOrDefault(t => t.talla_eu == size && t.id_marca == product.id_marca);
                        if (universalSize == null && brandSize == null)
                        {
                            invalidEntries.Add($"Fila {row}, Talla no válida: {size}.");
                            continue;
                        }
                    }
                }

                foreach (var entry in inventoryUpdates)
                {
                    var parts = entry.Key.Split('/');
                    var sku = parts[0];
                    var size = parts[1];
                    var stock = entry.Value.stock;
                    var price = entry.Value.price;

                    var existingInventory = db.Inventario.FirstOrDefault(i => i.id_producto == sku && i.talla_eu == size);
                    if (existingInventory != null)
                    {
                        existingInventory.cantidad += stock;
                        existingInventory.precio = price;
                        invalidEntries.Add($"SKU duplicado en la base de datos: {sku}, Talla: {size}. Existencias sumadas y precio actualizado.");
                    }
                    else
                    {
                        var producto = db.Productos.FirstOrDefault(p => p.id_producto == sku);
                        var newInventory = new Inventario
                        {
                            id_producto = sku,
                            id_marca = producto.id_marca,
                            talla_eu = size,
                            cantidad = stock,
                            precio = price,
                            fecha_actualizacion = DateTime.Now
                        };
                        db.Inventario.Add(newInventory);
                    }
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    invalidEntries
                }, JsonRequestBehavior.AllowGet);
            }
        }

        private int? AsignarIdModelo(string nombreProducto, int idMarca)
        {
            using (var context = new TFCEntities())
            {
                var modelos = context.Modelos.Where(m => m.id_marca == idMarca).ToList();

                foreach (var modelo in modelos)
                {
                    var palabrasModelo = modelo.nombre_modelo.Split(' ');

                    bool todasPalabrasPresentes = palabrasModelo.All(palabra =>
                        nombreProducto.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (todasPalabrasPresentes)
                        return modelo.id_modelo;
                }
            }

            return null;
        }

        private async Task<JObject> SearchProduct(string sku)
        {
            using (var client = new HttpClient())
            {
                var apiUrl = $"https://stockx-api.p.rapidapi.com/search?query={sku}"; 
                var searchRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(apiUrl),
                    Headers =
                    {
                        { "x-rapidapi-key", "a65cdaab74msh2a979964bac5ca6p1609e9jsna66da403c24f" },
                        { "x-rapidapi-host", "stockx-api.p.rapidapi.com" },
                    },
                };

                var response = await client.SendAsync(searchRequest);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var jsonData = JObject.Parse(body);

                return jsonData;
            }
        }

        private static async Task<int> ObtenerPrecioMedioMercado(decimal precio)
        {
            try
            {
                return await ConvertCurrency(precio, "USD", "EUR");
            }
            catch (Exception)
            {
                return await ConvertCurrency2(precio, "USD", "EUR");
            }
        }

        private static async Task<int> ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            var client = new RestClient("https://openexchangerates.org/api/");
            var request = new RestRequest("latest.json", Method.Get);
            request.AddParameter("app_id", "2bf904fcc3ca41d78557adc8325f2320");
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var jsonData = JObject.Parse(response.Content);
                var rates = jsonData["rates"];
                var fromRate = rates[fromCurrency].Value<decimal>();
                var toRate = rates[toCurrency].Value<decimal>();
                var conversionRate = toRate / fromRate;
                return (int)(amount * conversionRate);
            }
            else
                throw new Exception("Error al obtener las tasas de cambio.");
        }

        private static async Task<int> ConvertCurrency2(decimal amount, string fromCurrency, string toCurrency)
        {
            var client = new RestClient("https://v6.exchangerate-api.com/v6/74b2ae015e9fd5a65d8ba28d/latest/");
            var request = new RestRequest($"{fromCurrency}", Method.Get);
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var jsonData = JObject.Parse(response.Content);
                var rates = jsonData["conversion_rates"];
                var toRate = rates[toCurrency].Value<decimal>();
                return (int)(amount * toRate);
            }
            else
            {
                throw new Exception("Error al obtener las tasas de cambio.");
            }
        }
    }
}