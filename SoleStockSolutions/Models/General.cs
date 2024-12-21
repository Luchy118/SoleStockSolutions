using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SoleStockSolutions.Models
{
    public class RelatedProducts
    {
        public Productos Producto { get; set; }
        public int PrecioMinimo { get; set; }
    }

    public class ProductsDatatable
    {
        public string SKU { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string Nombre { get; set; }
        public DateTime? FechaLanzamiento { get; set; }
        public string Imagen { get; set; }
        public DateTime? FechaUltimoUsoInterno { get; set; }
        public int VecesVisto { get; set; }
    }

    public class ModelsDatatable
    {
        public int Id_Modelo { get; set; }
        public string Nombre_Modelo { get; set; }
        public int Id_Marca { get; set; }
        public string Nombre_Marca { get; set; }
    }

    public class SizesViewModel
    {
        public List<Tallas_Universales> UniversalSizes { get; set; }
        public List<BrandSizesDatatable> BrandSizes { get; set; }
    }

    public class BrandSizesDatatable
    {
        public string Talla_Eu { get; set; }
        public int Id_Marca { get; set; }
        public string Nombre_Marca { get; set; }
    }
}