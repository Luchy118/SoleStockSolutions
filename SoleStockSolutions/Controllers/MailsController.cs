using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Address = SoleStockSolutions.Models.Address;

namespace SoleStockSolutions.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class MailsController : Controller
    {
        public ActionResult OrderConfirmed()
        {
            using (var db = new TFCEntities())
            {
                var order = db.Pedidos.Include(o => o.Detalles_Pedidos).FirstOrDefault(o => o.id_pedido == "4166da7d-16b5-4967-9fc8-70bc37a47744");
                var billingAddress = db.Direcciones.FirstOrDefault(d => d.id_direccion == order.id_direccionFacturacion);
                var shippingAddress = db.Direcciones.FirstOrDefault(d => d.id_direccion == order.id_direccionEnvio);

                var orderDetails = new OrderDetailsViewModel
                {
                    OrderId = order.id_pedido,
                    OrderDate = order.fecha_pedido,
                    Total = order.total,
                    ShippingCost = (decimal)(order.total - order.Detalles_Pedidos.Sum(d => d.subtotal)),
                    Email = order.Usuarios.email,
                    FirstName = order.Usuarios.nombre,
                    LastName = order.Usuarios.apellidos,
                    PaymentMethod = order.metodo_pago,
                    CardType = order.tipo_tarjeta,
                    Last4 = order.last4,
                    BillingAddress = billingAddress != null ? new Address
                    {
                        AddressLine1 = billingAddress.direccion,
                        AddressLine2 = billingAddress.direccion2,
                        Country = billingAddress.pais,
                        Province = billingAddress.provincia,
                        City = billingAddress.ciudad,
                        PostalCode = billingAddress.codigo_postal,
                        PhoneNumber = billingAddress.telefono
                    } : null,
                    ShippingAddress = shippingAddress != null ? new Address
                    {
                        AddressLine1 = shippingAddress.direccion,
                        AddressLine2 = shippingAddress.direccion2,
                        Country = shippingAddress.pais,
                        Province = shippingAddress.provincia,
                        City = shippingAddress.ciudad,
                        PostalCode = shippingAddress.codigo_postal,
                        PhoneNumber = shippingAddress.telefono
                    } : null,
                    Items = order.Detalles_Pedidos.Select(d => new OrderItemViewModel
                    {
                        ProductId = d.id_producto,
                        ProductName = d.Productos.nombre,
                        ProductImageUrl = d.Productos.imagen,
                        ProductLink = Url.Action("ProductDetails", "Products", new { productName = d.Productos.nombre.Replace(" ", "-").Replace("(", "").Replace(")", "") }),
                        Size = d.id_talla,
                        Quantity = d.cantidad,
                        Price = d.precio_unitario,
                        Subtotal = (decimal)d.subtotal
                    }).ToList()
                };

                return View(orderDetails);
            }
        }

        public ActionResult VerifyAccount(string callbackUrl)
        {
            return View(callbackUrl);
        }

        public ActionResult OrderUpdates(string orderStatus, string orderNumber, string callbackUrl)
        {
            var model = new Tuple<string, string, string>(orderStatus, orderNumber, callbackUrl);
            return View(model);
        }

        public ActionResult ResetPassword()
        {
            return View();
        }
    }
}