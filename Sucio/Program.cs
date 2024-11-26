using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraerDatosAPI
{
    internal class Program
    {
        private static readonly TFCEntities db = new TFCEntities();

        static async Task Main(string[] args)
        {
            //await InsertarModelosDeZapatillas();
            //await AsignarModelosAProductos();
            //await InsertarTallasUniv();
            //await InsertarTallasMarca(db.Marcas.FirstOrDefault(m => m.nombre_marca.ToLower() == "adidas").id_marca);
            await ActualizarMarca();
            //await GenerarInventarioAleatorio();
        }

        private static async Task InsertarModelosDeZapatillas()
        {
            var marcas = new Dictionary<string, int>();
            var nombresMarcas = new List<string> { "Jordan", "Nike", "Adidas", "New balance", "Yeezy" };

            foreach (var nombreMarca in nombresMarcas)
            {
                var marca = await db.Marcas.FirstOrDefaultAsync(m => m.nombre_marca == nombreMarca);
                if (marca != null)
                    marcas[nombreMarca] = marca.id_marca;
            }

            var modelos = new List<Modelos>
                {
                    new Modelos { nombre_modelo = "Jordan 1 High", id_marca = marcas["Jordan"] },
                    new Modelos { nombre_modelo = "Jordan 1 Low", id_marca = marcas["Jordan"] },
                    new Modelos { nombre_modelo = "Jordan 3", id_marca = marcas["Jordan"] },
                    new Modelos { nombre_modelo = "Jordan 4", id_marca = marcas["Jordan"] },
                    new Modelos { nombre_modelo = "Jordan 11", id_marca = marcas["Jordan"] },
                    new Modelos { nombre_modelo = "Nike Dunk Low", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "Nike Dunk SB", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "Nike Air Force 1", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "Nike Air Max 1", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "Nike Air Max 95", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "Adidas Samba", id_marca = marcas["Adidas"] },
                    new Modelos { nombre_modelo = "Adidas Campus", id_marca = marcas["Adidas"] },
                    new Modelos { nombre_modelo = "Adidas Forum", id_marca = marcas["Adidas"] },
                    new Modelos { nombre_modelo = "Adidas Handball Spezial", id_marca = marcas["Adidas"] },
                    new Modelos { nombre_modelo = "Yeezy 350", id_marca = marcas["Yeezy"] },
                    new Modelos { nombre_modelo = "Yeezy 700", id_marca = marcas["Yeezy"] },
                    new Modelos { nombre_modelo = "Yeezy Slide", id_marca = marcas["Yeezy"] },
                    new Modelos { nombre_modelo = "Yeezy Foam RNNR", id_marca = marcas["Yeezy"] },
                    new Modelos { nombre_modelo = "New balance 550", id_marca = marcas["New balance"] },
                    new Modelos { nombre_modelo = "New balance 990", id_marca = marcas["New balance"] },
                    new Modelos { nombre_modelo = "New balance 1906D", id_marca = marcas["New balance"] },
                    new Modelos { nombre_modelo = "New balance 2002R", id_marca = marcas["New balance"] },
                    new Modelos { nombre_modelo = "Nike Air Rubber Dunk", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "Nike Air Force 3", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "Nike Air Alpha Force", id_marca = marcas["Nike"] },
                    new Modelos { nombre_modelo = "New balance 57/40", id_marca = marcas["New balance"] },
                    new Modelos { nombre_modelo = "New balance 327", id_marca = marcas["New balance"] },
                    new Modelos { nombre_modelo = "Yeezy Fleece Slide", id_marca = marcas["Yeezy"] },
                };

            db.Modelos.AddRange(modelos);
            await db.SaveChangesAsync();
        }

        private static async Task InsertarTallasUniv()
        {
            List<string> tallas_eu = new List<string>
            {
                "36", "36.5", "37.5", "38", "38.5", "39", "40", "40.5", "41", "42", "42.5", "43", "44", "44.5", "45", "45.5", "46", "47"
            };
            List<string> tallas_us = new List<string>
            {
                "4", "4.5", "5", "5.5", "6", "6.5", "7", "7.5", "8", "8.5", "9", "9.5", "10", "10.5", "11", "11.5", "12", "12.5"
            };
            List<string> tallas_uk = new List<string>
            {
                "3", "3.5", "4", "4.5", "5", "5.5", "6", "6.5", "7", "7.5", "8", "8.5", "9", "9.5", "10", "10.5", "11", "11.5"
            };

            foreach (var talla in tallas_eu)
            {
                var tallaUniv = new Tallas_Universales
                {
                    talla_eu = talla,
                    talla_us = tallas_us[tallas_eu.IndexOf(talla)],
                    talla_uk = tallas_uk[tallas_eu.IndexOf(talla)]
                };
                db.Tallas_Universales.Add(tallaUniv);
            }

            await db.SaveChangesAsync();
        }

        private static async Task InsertarTallasMarca(int id_marca)
        {
            List<string> tallas_eu = new List<string>
            {
                "36", "36 2/3", " 37 1/3", "38", "38 2/3", "39 1/3", "40", "40 2/3", "41 1/3", "42", "42 2/3", "43 1/3", "44", "44 2/3", "45 1/3", "46", "46 2/3", "47 1/3"
            };
            List<string> tallas_us = new List<string>
            {
                "4", "4.5", "5", "5.5", "6", "6.5", "7", "7.5", "8", "8.5", "9", "9.5", "10", "10.5", "11", "11.5", "12", "12.5"
            };
            List<string> tallas_uk = new List<string>
            {
                "3.5", "4", "4.5", "5", "5.5", "6", "6.5", "7", "7.5", "8", "8.5", "9", "9.5", "10", "10.5", "11", "11.5", "12"
            };

            foreach (var talla in tallas_eu)
            {
                var tallaMarca = new Tallas_Marcas
                {
                    id_marca = id_marca,
                    talla_eu = talla,
                    talla_us = tallas_us[tallas_eu.IndexOf(talla)],
                    talla_uk = tallas_uk[tallas_eu.IndexOf(talla)]
                };

                db.Tallas_Marcas.Add(tallaMarca);
            }

            await db.SaveChangesAsync();
        }

        private static async Task AsignarModelosAProductos()
        {
            var productos = await db.Productos.ToListAsync();
            var modelos = await db.Modelos.ToListAsync();

            foreach (var producto in productos.Where(p => p.id_modelo == null))
            {
                var modelo = modelos.FirstOrDefault(m => m.nombre_modelo.Split(' ').All(palabra => producto.nombre.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0));
                if (modelo != null)
                    producto.id_modelo = modelo.id_modelo;
            }

            await db.SaveChangesAsync();
        }

        private static async Task ActualizarMarca()
        {
            var marcaYeezy = await db.Marcas.FirstOrDefaultAsync(m => m.nombre_marca.ToLower() == "yeezy");
            var productos = await db.Inventario.Where(i => db.Productos.FirstOrDefault(p => p.id_producto == i.id_producto).nombre.ToLower().Contains("yeezy")).ToListAsync();

            foreach (var producto in productos)
                producto.id_marca = marcaYeezy.id_marca;

            await db.SaveChangesAsync();
        }
        private static async Task GenerarInventarioAleatorio()
        {
            var random = new Random();
            var productos = await db.Productos.OrderBy(p => Guid.NewGuid()).Take(100).ToListAsync();
            var tallasUniversales = await db.Tallas_Universales.ToListAsync();
            var tallasMarcas = await db.Tallas_Marcas.ToListAsync();

            foreach (var producto in productos)
            {
                bool condicionTallaMarca(Tallas_Marcas t) =>
                    t.id_marca == producto.id_marca ||
                    producto.nombre.ToLower().Contains(db.Marcas.FirstOrDefault(m => m.id_marca == t.id_marca).nombre_marca.ToLower());

                var existeTallaMarca = tallasMarcas.Any(condicionTallaMarca);

                var tallas = existeTallaMarca
                    ? tallasMarcas.Where(condicionTallaMarca)
                                  .OrderBy(t => Guid.NewGuid())
                                  .Take(random.Next(1, 8))
                                  .Select(t => t.talla_eu)
                                  .ToList()
                    : tallasUniversales.OrderBy(t => Guid.NewGuid())
                                       .Take(random.Next(1, 8))
                                       .Select(t => t.talla_eu)
                                       .ToList();

                foreach (var talla in tallas)
                {
                    if (existeTallaMarca && !tallasMarcas.Any(t => condicionTallaMarca(t) && t.talla_eu == talla))
                        continue;

                    if (!existeTallaMarca && !tallasUniversales.Any(t => t.talla_eu == talla))
                        continue;

                    var inventarioExistente = db.Inventario.FirstOrDefault(i => i.id_producto == producto.id_producto && (i.talla_eu == talla || i.talla_eu_marca == talla));

                    if (inventarioExistente != null)
                    {
                        inventarioExistente.cantidad += random.Next(1, 6);
                        inventarioExistente.fecha_actualizacion = DateTime.Now;
                    }
                    else
                    {
                        var inventario = new Inventario
                        {
                            id_producto = producto.id_producto,
                            id_marca = producto.nombre.ToLower().Contains("yeezy") ? db.Marcas.FirstOrDefault(m => m.nombre_marca.ToLower() == "yeezy").id_marca : producto.id_marca,
                            talla_eu = !existeTallaMarca ? talla : null,
                            talla_eu_marca = existeTallaMarca ? talla : null,
                            cantidad = random.Next(1, 6),
                            precio = (int)(producto.precio_medio_mercado.HasValue ? Math.Round(producto.precio_medio_mercado.Value / 5.0m) * 5 + 20 : 0),
                            fecha_actualizacion = DateTime.Now
                        };

                        if (existeTallaMarca)
                        {
                            var tallaMarca = tallasMarcas.FirstOrDefault(t => condicionTallaMarca(t) && t.talla_eu == talla);
                            if (tallaMarca != null) inventario.Tallas_Marcas = tallaMarca;
                        }
                        else
                        {
                            var tallaUniversal = tallasUniversales.FirstOrDefault(t => t.talla_eu == talla);
                            if (tallaUniversal != null) inventario.Tallas_Universales = tallaUniversal;
                        }

                        db.Inventario.Add(inventario);
                    }
                }
            }

            await db.SaveChangesAsync();
        }

    }
}
