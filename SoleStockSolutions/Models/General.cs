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

    public class InventoryDatatable
    {
        public string SKU { get; set; }
        public string Image { get; set; }
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public int TotalStock { get; set; }
        public decimal MarketValue { get; set; }
        public decimal TotalSales { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class SizeStockPrice
    {
        public string Size { get; set; }
        public int Stock { get; set; }
        public int Price { get; set; }
    }

    public class SearchResult
    {
        public string id_producto { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public int id_marca { get; set; }
        public int id_modelo { get; set; }
        public string imagen { get; set; }
        public DateTime fecha_lanzamiento { get; set; }
        public int precio_medio_mercado { get; set; }
    }

    public class CartItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal Total => Items.Sum(item => item.Price * item.Quantity);
    }
}