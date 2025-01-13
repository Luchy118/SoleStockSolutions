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
        public string Size { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int MaxStock { get; set; }
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal Total => Items.Sum(item => item.Price * item.Quantity);
    }

    public class UserAddresses
    {
        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }
    }

    public class Address
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Country { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class OrderViewModel
    {
        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }
        public string ShippingOption { get; set; }
        public decimal ShippingCost { get; set; }
        public List<CartItem> CartItems { get; set; }
        public string Token { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class EstadoIds
    {
        public static readonly int Pendiente;
        public static readonly int Confirmado;
        public static readonly int Procesando;
        public static readonly int Enviado;
        public static readonly int EnCamino;
        public static readonly int Entregado;
        public static readonly int Cancelado;
        public static readonly int Devuelto;
        public static readonly int ReembolsoEnProceso;
        public static readonly int Reembolsado;
        public static readonly int EnEspera;
        public static readonly int PreparadoParaEnvio;
        public static readonly int NoEntregado;
        public static readonly int Recogido;

        static EstadoIds()
        {
            using (var db = new TFCEntities())
            {
                Pendiente = db.Estados.First(e => e.nombre_estado == "Pendiente").id_estado;
                Confirmado = db.Estados.First(e => e.nombre_estado == "Confirmado").id_estado;
                Procesando = db.Estados.First(e => e.nombre_estado == "Procesando").id_estado;
                Enviado = db.Estados.First(e => e.nombre_estado == "Enviado").id_estado;
                EnCamino = db.Estados.First(e => e.nombre_estado == "En camino").id_estado;
                Entregado = db.Estados.First(e => e.nombre_estado == "Entregado").id_estado;
                Cancelado = db.Estados.First(e => e.nombre_estado == "Cancelado").id_estado;
                Devuelto = db.Estados.First(e => e.nombre_estado == "Devuelto").id_estado;
                ReembolsoEnProceso = db.Estados.First(e => e.nombre_estado == "Reembolso en proceso").id_estado;
                Reembolsado = db.Estados.First(e => e.nombre_estado == "Reembolsado").id_estado;
                EnEspera = db.Estados.First(e => e.nombre_estado == "En espera").id_estado;
                PreparadoParaEnvio = db.Estados.First(e => e.nombre_estado == "Preparado para envío").id_estado;
                NoEntregado = db.Estados.First(e => e.nombre_estado == "No entregado").id_estado;
                Recogido = db.Estados.First(e => e.nombre_estado == "Recogido").id_estado;
            }
        }
    }

    public class OrderDetailsViewModel
    {
        public string OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
        public decimal ShippingCost { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PaymentMethod { get; set; }
        public string Last4 { get; set; }
        public string CardType { get; set; }
        public Address BillingAddress { get; set; }
        public Address ShippingAddress { get; set; }
        public List<OrderItemViewModel> Items { get; set; }
        public List<Actualizaciones_Pedidos> OrderUpdates { get; set; }
    }

    public class OrderItemViewModel
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public string ProductLink { get; set; }
        public string Size { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class LineStatusUpdate
    {
        public string ProductId { get; set; }
        public int Status { get; set; }
    }

    public class WishlistViewModel
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal LowestPrice { get; set; }
        public bool InStock { get; set; }
    }

    public class EstadoCategoriaViewModel
    {
        public List<Estados> Estados { get; set; }
        public List<Categorías_Estados> Categorias { get; set; }
    }
}