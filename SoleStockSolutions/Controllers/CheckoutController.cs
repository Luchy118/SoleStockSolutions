using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Address = SoleStockSolutions.Models.Address;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using Stripe.Climate;
using Microsoft.Ajax.Utilities;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    public class CheckoutController : Controller
    {
        private List<CartItem> GetCartItems()
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItems = cart.Items.ToList();
            return cartItems;
        }

        public ActionResult Index(string productId = null, string size = null, int quantity = 0)
        {
            using (var db = new TFCEntities())
            {
                ViewBag.CurrentAction = "Checkout";

                if (!string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(size) && quantity > 0)
                {
                    Session["Cart"] = new Cart();
                    AddToCart(productId.ToUpper(), size, quantity);
                }

                var cartItems = GetCartItems();
                if (!cartItems.Any())
                    return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));

                if (User.Identity.IsAuthenticated)
                {
                    var userId = db.Usuarios.First(u => u.email == User.Identity.Name).id_usuario;
                    var userAddresses = GetUserAddresses(db, userId);
                    ViewBag.BillingAddress = userAddresses?.BillingAddress;
                    ViewBag.ShippingAddress = userAddresses?.ShippingAddress;
                }

                return View(cartItems);
            }
        }

        public ActionResult Cart()
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItems = cart.Items.ToList();
            ViewBag.CurrentAction = "Cart";
            return View(cartItems);
        }

        [HttpPost]
        public ActionResult AddToCart(string productId, string size, int quantity)
        {
            var db = new TFCEntities();

            var product = db.Productos.FirstOrDefault(p => p.id_producto == productId);
            var productInInventory = db.Inventario.FirstOrDefault(i => i.id_producto == productId && (i.talla_eu == size || i.talla_eu_marca == size));

            db.Dispose();

            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(item => item.ProductId == productId && item.Size == size);

            if (cartItem == null)
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.nombre,
                    Size = size,
                    ImageUrl = product.imagen,
                    Quantity = quantity,
                    Price = productInInventory.precio,
                    MaxStock = productInInventory.cantidad
                });
            }
            else
            {
                if (cartItem.Quantity >= cartItem.MaxStock)
                    return Json(new { success = false, message = "El producto ya se encuentra en el carrito con las existencias máximas." });

                cartItem.Quantity += quantity;
                if (cartItem.Quantity > cartItem.MaxStock)
                {
                    cartItem.Quantity = cartItem.MaxStock;
                }
            }

            Session["Cart"] = cart;
            return PartialView("_CartPartial", cart);
        }

        [HttpPost]
        public ActionResult UpdateQuantity(string productId, string size, int change)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.Size == size);
            if (cartItem != null)
            {
                cartItem.Quantity += change;
                if (cartItem.Quantity < 1)
                    cartItem.Quantity = 1;
                else if (cartItem.Quantity > cartItem.MaxStock)
                    cartItem.Quantity = cartItem.MaxStock;
            }

            return PartialView("_CartPartial", cart);
        }

        [HttpPost]
        public ActionResult RemoveFromCart(string productId, string size, bool cartPage = false)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(item => item.ProductId == productId && item.Size == size);

            if (cartItem != null)
                cart.Items.Remove(cartItem);

            Session["Cart"] = cart;

            if (!cartPage)
                return PartialView("_CartPartial", cart);
            else
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpdateCart(List<int> quantities)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();

            for (int i = 0; i < cart.Items.Count; i++)
            {
                if (i < quantities.Count)
                {
                    if (quantities[i] == 0)
                        cart.Items.Remove(cart.Items[i]);
                    else
                        cart.Items[i].Quantity = quantities[i];
                }
            }

            Session["Cart"] = cart;
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public UserAddresses GetUserAddresses(TFCEntities db, int userId)
        {
            var billingAddress = db.Direcciones.FirstOrDefault(d => d.id_usuario == userId && d.predeterminada && d.tipo_direccion == 1);
            var shippingAddress = db.Direcciones.FirstOrDefault(d => d.id_usuario == userId && d.predeterminada && d.tipo_direccion == 0);

            var userAddresses = new UserAddresses
            {
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
                } : null
            };

            return userAddresses;
        }

        [HttpPost]
        public async Task<ActionResult> ProcessOrderAsync(OrderViewModel order)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Datos del pedido inválidos." }, JsonRequestBehavior.AllowGet);

            using (var db = new TFCEntities())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(order.Email))
                        {
                            bool isEmailValid = await VerifyEmailAsync(order.Email);
                            if (!isEmailValid)
                                return Json(new { success = false, message = "El correo electrónico no es válido." });
                        }

                        StripeConfiguration.ApiKey = ConfigurationManager.AppSettings["StripeSecretKey"];
                        var OrderId = Guid.NewGuid().ToString();

                        var metadata = new Dictionary<string, string>
                        {
                            { "OrderId", OrderId },
                            { "UserId", User.Identity.IsAuthenticated ? User.Identity.Name : "Guest" }
                        };

                        if (!string.IsNullOrEmpty(order.FirstName) && !string.IsNullOrEmpty(order.LastName))
                            metadata.Add("ContactName", $"{order.FirstName} {order.LastName}");

                        if (!string.IsNullOrEmpty(order.Email))
                            metadata.Add("ContactEmail", order.Email);

                        var options = new ChargeCreateOptions
                        {
                            Amount = (long)((order.CartItems.Sum(item => item.Quantity * item.Price) + order.ShippingCost) * 100),
                            Currency = "eur",
                            Description = "Compra en SoleStockSolutions",
                            Source = order.Token,
                            Metadata = metadata
                        };

                        var service = new ChargeService();
                        Charge charge = service.Create(options);

                        if (charge.Status != "succeeded")
                            return Json(new { success = false, message = "Error al procesar el pago." }, JsonRequestBehavior.AllowGet);

                        int userId;
                        bool existsGuest = db.Usuarios.Any(u => u.email == order.Email);
                        bool existsNotGuest = db.Usuarios.Any(u => u.email == User.Identity.Name);
                        if (existsGuest)
                        {
                            userId = db.Usuarios.First(u => u.email == order.Email).id_usuario;
                        }
                        else if (existsNotGuest)
                        {
                            userId = db.Usuarios.First(u => u.email == User.Identity.Name).id_usuario;
                        }
                        else
                        {
                            var guestUser = new Usuarios
                            {
                                nombre = order.FirstName,
                                apellidos = order.LastName,
                                email = order.Email,
                                guest = true
                            };
                            db.Usuarios.Add(guestUser);
                            db.SaveChanges();
                            userId = guestUser.id_usuario;
                        }

                        var billingAddressId = SaveAddress(db, userId, order.BillingAddress, true);
                        var shippingAddressId = SaveAddress(db, userId, order.ShippingAddress, false);

                        var newOrder = new Pedidos
                        {
                            id_pedido = OrderId,
                            id_usuario = userId,
                            fecha_pedido = DateTime.Now,
                            total = order.CartItems.Sum(item => item.Quantity * item.Price) + order.ShippingCost,
                            id_estado = EstadoIds.Confirmado,
                            metodo_pago = "Tarjeta",
                            id_direccionEnvio = shippingAddressId,
                            id_direccionFacturacion = billingAddressId,
                            last4 = charge.PaymentMethodDetails.Card.Last4,
                            tipo_tarjeta = charge.PaymentMethodDetails.Card.Brand,
                            precio_envio = order.ShippingCost,
                            Actualizaciones_Pedidos = new List<Actualizaciones_Pedidos>
                            {
                                new Actualizaciones_Pedidos
                                {
                                    id_pedido = OrderId,
                                    id_estado = EstadoIds.Confirmado,
                                    fecha = DateTime.Now,
                                    descripcion = "El pedido se ha recibido correctamente."
                                }
                            }
                        };

                        db.Pedidos.Add(newOrder);
                        db.SaveChanges();

                        foreach (var item in order.CartItems)
                        {
                            db.Inventario.FirstOrDefault(i => i.id_producto == item.ProductId).veces_vendido += item.Quantity;

                            var uniObj = db.Tallas_Universales.FirstOrDefault(t => t.talla_eu == item.Size);

                            string uniSize = null;
                            if (uniObj != null)
                                uniSize = uniObj.talla_eu;

                            var brandSize = uniSize ?? db.Tallas_Marcas.FirstOrDefault(t => t.talla_eu == item.Size).talla_eu;

                            var orderDetail = new Detalles_Pedidos
                            {
                                id_pedido = newOrder.id_pedido,
                                id_producto = item.ProductId,
                                id_talla = uniSize ?? brandSize,
                                cantidad = item.Quantity,
                                precio_unitario = item.Price,
                                subtotal = item.Quantity * item.Price,
                                estado_linea = EstadoIds.Pendiente
                            };

                            db.Detalles_Pedidos.Add(orderDetail);

                            var inventoryItem = db.Inventario.First(i => i.id_producto == item.ProductId && (i.talla_eu == item.Size || i.talla_eu_marca == item.Size));
                            inventoryItem.cantidad -= item.Quantity;
                            db.Entry(inventoryItem).State = EntityState.Modified;
                        }

                        db.SaveChanges();

                        transaction.Commit();

                        EnviarCorreoConfirmacion(order, newOrder.id_pedido);

                        Session["Cart"] = new Cart();

                        return Json(new { success = true, redirectUrl = Url.Action("OrderConfirmed", new { orderId = newOrder.id_pedido }) }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Error al procesar el pedido: " + ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }

        private int SaveAddress(TFCEntities db, int userId, Address address, bool isBilling)
        {
            var addressType = isBilling ? 1 : 0;
            var addressPrefix = isBilling ? "Dirección de Facturación" : "Dirección de Envío";
            var existingAddress = db.Direcciones.FirstOrDefault(d => d.id_usuario == userId && d.tipo_direccion == addressType && d.predeterminada) ?? db.Direcciones.FirstOrDefault(d => d.id_usuario == userId && d.tipo_direccion == addressType);

            if (existingAddress == null)
            {
                int addressNumber = 1;
                string addressName;

                do
                {
                    addressName = $"{addressPrefix} {addressNumber}";
                    addressNumber++;
                } while (db.Direcciones.Any(d => d.id_usuario == userId && d.nombre == addressName));

                var newAddress = new Direcciones
                {
                    id_usuario = userId,
                    direccion = address.AddressLine1,
                    direccion2 = address.AddressLine2,
                    pais = address.Country,
                    provincia = address.Province,
                    ciudad = address.City,
                    codigo_postal = address.PostalCode,
                    telefono = address.PhoneNumber,
                    tipo_direccion = (byte)addressType,
                    predeterminada = true,
                    nombre = addressName
                };

                db.Direcciones.Add(newAddress);
                db.SaveChanges();

                return newAddress.id_direccion;
            }

            return existingAddress.id_direccion;
        }

        public ActionResult OrderConfirmed(string orderId)
        {
            using (var db = new TFCEntities())
            {
                var order = db.Pedidos.Include(o => o.Detalles_Pedidos).FirstOrDefault(o => o.id_pedido == orderId) ?? throw new HttpException(404, "Página no encontrada");
                var billingAddress = db.Direcciones.FirstOrDefault(d => d.id_direccion == order.id_direccionFacturacion);
                var shippingAddress = db.Direcciones.FirstOrDefault(d => d.id_direccion == order.id_direccionEnvio);
                var user = db.Usuarios.First(u => u.id_usuario == order.id_usuario);

                var orderDetails = new OrderDetailsViewModel
                {
                    OrderId = order.id_pedido,
                    OrderDate = order.fecha_pedido,
                    Total = order.total,
                    ShippingCost = (decimal)(order.total - order.Detalles_Pedidos.Sum(d => d.subtotal)),
                    FirstName = user.nombre,
                    LastName = user.apellidos,
                    Email = user.email,
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

        private void EnviarCorreoConfirmacion(OrderViewModel order, string orderId)
        {
            var fromAddress = new MailAddress("solestocksolutions@gmail.com", "Sole Stock Solutions");
            var toAddress = new MailAddress(!string.IsNullOrEmpty(User.Identity.Name) ? User.Identity.Name : order.Email);
            const string fromPassword = "mbbe uhmn vcaz ktbg";
            string subject = $"Confirmación de su pedido: {orderId}";

            var db = new TFCEntities(); 

            var auxProperties = new
            {
                db.Pedidos.First(p => p.id_pedido == orderId).Usuarios.email,
                db.Pedidos.First(p => p.id_pedido == orderId).Usuarios.nombre,
                db.Pedidos.First(p => p.id_pedido == orderId).Usuarios.apellidos,
                db.Pedidos.First(p => p.id_pedido == orderId).metodo_pago,
                db.Pedidos.First(p => p.id_pedido == orderId).tipo_tarjeta,
                db.Pedidos.First(p => p.id_pedido == orderId).last4
            };

            db.Dispose();

            var orderDetails = new OrderDetailsViewModel
            {
                OrderId = orderId,
                OrderDate = DateTime.Now,
                Total = order.CartItems.Sum(item => item.Quantity * item.Price) + order.ShippingCost,
                ShippingCost = order.ShippingCost,
                BillingAddress = order.BillingAddress,
                ShippingAddress = order.ShippingAddress,
                FirstName = auxProperties.nombre,
                LastName = auxProperties.apellidos,
                Email = auxProperties.email,
                PaymentMethod = auxProperties.metodo_pago,
                CardType = auxProperties.tipo_tarjeta,
                Last4 = auxProperties.last4,
                Items = order.CartItems.Select(item => new OrderItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductImageUrl = item.ImageUrl,
                    ProductLink = Url.Action("ProductDetails", "Products", new { productName = item.ProductName.Replace(" ", "-").Replace("(", "").Replace(")", "") }),
                    Size = item.Size,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Subtotal = item.Quantity * item.Price
                }).ToList()
            };

            string body = this.RenderViewToString("~/Views/Mails/OrderConfirmed.cshtml", orderDetails);

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }

        private async Task<bool> VerifyEmailAsync(string email)
        {
            string apiKey = "fa5a05fb590f01868d259e68fabfd9f7-7113c52e-a22e55b3";
            string url = $"https://api.mailgun.net/v4/address/validate?address={email}";

            using (HttpClient client = new HttpClient())
            {
                string base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64String);

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<MailgunResponse>(responseContent);
                    return result.IsValid;
                }
            }

            return false;
        }

        private class MailgunResponse
        {
            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("engagement")]
            public Engagement Engagement { get; set; }

            [JsonProperty("is_disposable_address")]
            public bool IsDisposableAddress { get; set; }

            [JsonProperty("is_role_address")]
            public bool IsRoleAddress { get; set; }

            [JsonProperty("reason")]
            public List<string> Reason { get; set; }

            [JsonProperty("result")]
            public string Result { get; set; }

            [JsonProperty("risk")]
            public string Risk { get; set; }

            public bool IsValid
            {
                get
                {
                    return !Engagement.IsBot && Result == "deliverable" && !IsDisposableAddress;
                }
            }
        }

        private class Engagement
        {
            [JsonProperty("engaging")]
            public bool Engaging { get; set; }

            [JsonProperty("is_bot")]
            public bool IsBot { get; set; }
        }

        ///// <summary>
        ///// Verifica la validez de un correo electrónico utilizando el servicio ZeroBounce.
        ///// </summary>
        ///// <param name="email">Correo electrónico a verificar.</param>
        ///// <returns>True si el correo electrónico es válido, de lo contrario false.</returns>
        //public static async Task<bool> VerifyEmailAsync(string email)
        //{
        //    string apiKey = "36acb9831cb24b999516f9aeb409d814";
        //    string url = $"https://api.zerobounce.net/v2/validate?api_key={apiKey}&email={email}";

        //    using (HttpClient client = new HttpClient())
        //    {
        //        HttpResponseMessage response = await client.GetAsync(url);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var result = await response.Content.ReadAsAsync<ZeroBounceResponse>();
        //            return result.Status == "valid";
        //        }
        //    }

        //    return false;
        //}

        ///// <summary>
        ///// Clase para deserializar la respuesta del servicio ZeroBounce.
        ///// </summary>
        //private class ZeroBounceResponse
        //{
        //    /// <summary>
        //    /// Estado de la validación del correo electrónico.
        //    /// </summary>
        //    public string Status { get; set; }
        //}
    }
}