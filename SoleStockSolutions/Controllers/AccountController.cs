using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Newtonsoft.Json;
using OfficeOpenXml;
using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    [CustomAuthorize(Roles = "User")]
    public class AccountController : Controller
    {
        /// <summary>
        /// Muestra la vista de inicio de sesión.
        /// </summary>
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index");

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CurrentAction = "LogIn";
            return View();
        }

        /// <summary>
        /// Muestra la vista de registro.
        /// </summary>
        [AllowAnonymous]
        public ActionResult SignUp()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index");

            ViewBag.CurrentAction = "SignUp";
            return View();
        }

        /// <summary>
        /// Muestra la vista principal de la cuenta.
        /// </summary>
        public ActionResult Index()
        {
            ViewBag.CurrentAction = "AccountIndex";
            return View();
        }

        /// <summary>
        /// Muestra la vista de la lista de deseos.
        /// </summary>
        public ActionResult Wishlist()
        {
            using (var db = new TFCEntities())
            {
                int userId = db.Usuarios.First(u => u.email == User.Identity.Name).id_usuario;
                var wishlistItems = db.Wishlist
                    .Where(w => w.id_usuario == userId)
                    .Select(w => new WishlistViewModel
                    {
                        ProductId = w.Productos.id_producto,
                        ProductName = w.Productos.nombre,
                        ImageUrl = w.Productos.imagen,
                        InStock = w.Productos.Inventario.Sum(i => i.cantidad) > 0,
                        LowestPrice = db.Inventario
                            .Where(i => i.id_producto == w.Productos.id_producto)
                            .OrderBy(i => i.precio)
                            .Select(i => i.precio)
                            .FirstOrDefault()
                    }).ToList();

                ViewBag.CurrentAction = "Wishlist";
                return View(wishlistItems);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult AddToWishlist(string productId)
        {
            using (var db = new TFCEntities())
            {
                try
                {
                    int userId = db.Usuarios.First(u => u.email == User.Identity.Name).id_usuario;
                    var existingWishlistItem = db.Wishlist.FirstOrDefault(w => w.id_producto == productId && w.id_usuario == userId);

                    if (existingWishlistItem == null)
                    {
                        var wishlistItem = new Wishlist
                        {
                            id_usuario = userId,
                            id_producto = productId
                        };
                        db.Wishlist.Add(wishlistItem);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new { success = false, message = "El producto ya se encuentra en la lista de deseos." }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public ActionResult RemoveFromWishlist(string productId)
        {
            using (var db = new TFCEntities())
            {
                try
                {
                    var wishlistItem = db.Wishlist.FirstOrDefault(w => w.id_producto == productId && w.Usuarios.email == User.Identity.Name);
                    if (wishlistItem != null)
                    {
                        db.Wishlist.Remove(wishlistItem);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        /// <summary>
        /// Cierra la sesión del usuario.
        /// </summary>
        [HttpGet]
        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Valida las credenciales de inicio de sesión del usuario.
        /// </summary>
        /// <param name="u">Objeto Usuarios con las credenciales del usuario.</param>
        /// <param name="returnUrl">URL de retorno después del inicio de sesión.</param>
        /// <returns>Resultado de la validación en formato JSON.</returns>
        [HttpPost]
        [AllowAnonymous]
        public JsonResult ValidateLogin(Usuarios u, string returnUrl)
        {
            using (var db = new TFCEntities())
            {
                Usuarios existe = db.Usuarios.FirstOrDefault(x => x.email == u.email && !x.guest);

                if (existe != null && existe.verificado)
                {
                    if (VerifyPassword(u.contrasenia, existe.contrasenia))
                    {
                        string roles = GetRoleForUser(existe);

                        FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(
                            1,
                            existe.email,
                            DateTime.Now,
                            DateTime.Now.AddMinutes(30),
                            false,
                            roles
                        );

                        string encryptedTicket = FormsAuthentication.Encrypt(authTicket);

                        HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                        Response.Cookies.Add(authCookie);

                        if (Url.IsLocalUrl(returnUrl) && !string.IsNullOrEmpty(returnUrl))
                            return Json(new { success = true, returnUrl }, JsonRequestBehavior.AllowGet);
                        else
                            return Json(new { success = true, returnUrl = Url.Action("Index", "Home") }, JsonRequestBehavior.AllowGet);
                    }
                    else
                        return Json(new { success = false, message = "La contraseña es errónea." }, JsonRequestBehavior.AllowGet);
                }
                else if (existe != null && !existe.verificado && !existe.baja)
                {
                    return Json(new { success = false, message = "Verifique su cuenta para continuar." }, JsonRequestBehavior.AllowGet);
                }
                else
                    return Json(new { success = false, message = "No existe ninguna cuenta registrada con ese correo electrónico." }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Valida el registro de un nuevo usuario.
        /// </summary>
        /// <param name="u">Objeto Usuarios con los datos del nuevo usuario.</param>
        /// <returns>Resultado de la validación en formato JSON.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> ValidateSignUp(Usuarios u)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Por favor, corrija los errores en el formulario." });

            using (var db = new TFCEntities())
            {
                Usuarios existe = db.Usuarios.FirstOrDefault(x => x.email == u.email);

                if (existe != null && !existe.guest && !existe.baja)
                    return Json(new { success = false, message = "El correo electrónico ya está en uso." });
                else if (existe != null && (existe.guest || existe.baja))
                {
                    bool isEmailValid = await VerifyEmailAsync(u.email);
                    if (!isEmailValid)
                        return Json(new { success = false, message = "El correo electrónico no es válido." });

                    existe.nombre = u.nombre;
                    existe.apellidos = u.apellidos;
                    existe.contrasenia = HashPassword(u.contrasenia);
                    existe.fecha_registro = DateTime.Now;
                    existe.guest = false;
                    existe.baja = false;
                    existe.fecha_baja = null;

                    db.Entry(existe).State = EntityState.Modified;
                    SendVerificationEmail(existe.email);
                }
                else
                {
                    bool isEmailValid = await VerifyEmailAsync(u.email);
                    if (!isEmailValid)
                        return Json(new { success = false, message = "El correo electrónico no es válido." });

                    u.contrasenia = HashPassword(u.contrasenia);
                    u.fecha_registro = DateTime.Now;

                    db.Usuarios.Add(u);
                    db.SaveChanges();
                    SendVerificationEmail(u.email);
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Se ha registrado correctamente, debe verificar su cuenta para iniciar sesión." });
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
        //private async Task<bool> VerifyEmailAsync(string email)
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

        /// <summary>
        /// Genera un hash SHA-256 para una contraseña.
        /// </summary>
        /// <param name="password">Contraseña a hashear.</param>
        /// <returns>Hash de la contraseña.</returns>
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Verifica si una contraseña coincide con su hash almacenado.
        /// </summary>
        /// <param name="inputPassword">Contraseña ingresada.</param>
        /// <param name="storedHash">Hash almacenado de la contraseña.</param>
        /// <returns>True si la contraseña coincide, de lo contrario false.</returns>
        public bool VerifyPassword(string inputPassword, string storedHash)
        {
            string inputHash = HashPassword(inputPassword);
            return inputHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene los roles de un usuario.
        /// </summary>
        /// <param name="user">Objeto Usuarios.</param>
        /// <returns>Cadena con los roles del usuario.</returns>
        private string GetRoleForUser(Usuarios user)
        {
            List<string> roles = new List<string>();

            if (user.super_administrador)
                roles.Add("SuperAdmin");
            if (user.administrador)
                roles.Add("Admin");

            roles.Add("User");

            return string.Join(",", roles);
        }

        public ActionResult ChangePassword()
        {
            ViewBag.CurrentAction = "ChangePassword";
            return View();
        }

        public ActionResult Addresses()
        {
            using (var db = new TFCEntities())
            {
                ViewBag.CurrentAction = "Addresses";

                var usuarioActual = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name);
                var direcciones = db.Direcciones.Where(d => d.id_usuario == usuarioActual.id_usuario).ToList();
                
                ViewBag.Direcciones = direcciones;

                return View();
            }
        }

        [HttpPost]
        public JsonResult AddAddress([Bind(Include = "nombre,direccion,direccion2,ciudad,codigo_postal,pais,telefono,tipo_direccion,predeterminada,provincia")] Direcciones objDireccion)
        {
            if (ModelState.IsValid)
            {
                using (var db = new TFCEntities())
                {
                    var usuarioActual = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name);
                    objDireccion.id_usuario = usuarioActual.id_usuario;
                    db.Direcciones.Add(objDireccion);

                    if (objDireccion.predeterminada)
                    {
                        var rmDefault = db.Direcciones.FirstOrDefault(d => d.predeterminada == true && d.tipo_direccion == objDireccion.tipo_direccion && d.id_usuario == usuarioActual.id_usuario);
                        if (rmDefault != null)
                            rmDefault.predeterminada = false;
                    }

                    db.SaveChanges();
                }

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).First() }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAddressById(int id)
        {
            using (var db = new TFCEntities())
            {
                var direccion = db.Direcciones
                    .Where(d => d.id_direccion == id)
                    .Select(d => new
                    {
                        d.id_direccion,
                        d.nombre,
                        d.direccion,
                        d.direccion2,
                        d.pais,
                        d.provincia,
                        d.ciudad,
                        d.codigo_postal,
                        d.telefono,
                        d.tipo_direccion,
                        d.predeterminada
                    })
                    .FirstOrDefault();

                if (direccion == null)
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, data = direccion }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult DeleteAddress(int id)
        {
            using (var db = new TFCEntities())
            {
                try
                {
                    var direccion = db.Direcciones.Find(id);
                    if (direccion != null)
                    {
                        db.Direcciones.Remove(direccion);
                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public JsonResult UpdateAddress(Direcciones model)
        {
            using (var db = new TFCEntities())
            {
                if (ModelState.IsValid)
                {
                    var direccion = db.Direcciones.FirstOrDefault(m => m.nombre == model.nombre);
                    if (direccion != null)
                    {
                        direccion.nombre = model.nombre;
                        direccion.direccion = model.direccion;
                        direccion.direccion2 = model.direccion2;
                        direccion.codigo_postal = model.codigo_postal;
                        direccion.ciudad = model.ciudad;
                        direccion.pais = model.pais;
                        direccion.telefono = model.telefono;
                        direccion.predeterminada = model.predeterminada;
                        direccion.tipo_direccion = model.tipo_direccion;

                        db.SaveChanges();
                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).First() }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult CheckAddressName(string nombre)
        {
            using (var db = new TFCEntities())
            {
                var exists = db.Direcciones.Any(d => d.nombre == nombre);
                return Json(new { exists }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Settings()
        {
            using (var db = new TFCEntities())
            {
                var user = db.Usuarios.First(u => u.email == User.Identity.Name);

                return View(user);
            }
        }

        [HttpPost]
        public JsonResult UpdatePersonalInfo(string firstName, string lastName)
        {
            using (var db = new TFCEntities())
            {
                try
                {
                    var user = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name);
                    if (user != null)
                    {
                        user.nombre = firstName;
                        user.apellidos = lastName;
                        db.SaveChanges();
                        return Json(new { success = true, message = "Información personal actualizada correctamente." }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public JsonResult ChangePassword(string currentPassword, string newPassword)
        {
            using (var db = new TFCEntities())
            {
                try
                {
                    var user = db.Usuarios.FirstOrDefault(u => u.email == User.Identity.Name);
                    if (user != null)
                    {
                        if (VerifyPassword(currentPassword, user.contrasenia))
                        {
                            if (VerifyPassword(newPassword, user.contrasenia))
                                return Json(new { success = false, message = "La nueva contraseña no puede ser igual a la actual." }, JsonRequestBehavior.AllowGet);

                            user.contrasenia = HashPassword(newPassword);
                            db.SaveChanges();
                            return Json(new { success = true, message = "Contraseña actualizada correctamente." }, JsonRequestBehavior.AllowGet);
                        }
                        return Json(new { success = false, message = "La contraseña actual es incorrecta." }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public JsonResult SendVerificationEmail(string userEmail)
        {
            var token = GenerateVerificationToken(userEmail);
            var callbackUrl = Url.Action("VerifyEmail", "Account", new { token }, protocol: Request.Url.Scheme).ToLower();

            var fromAddress = new MailAddress("solestocksolutions@gmail.com", "Sole Stock Solutions");
            var toAddress = new MailAddress(userEmail);
            const string fromPassword = "mbbe uhmn vcaz ktbg";
            string subject = "Verifique su cuenta";
            string body = this.RenderViewToString("~/Views/Mails/VerifyAccount.cshtml", callbackUrl);

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

            return Json(new { success = true, message = "Correo de verificación enviado correctamente." });
        }

        private string GenerateVerificationToken(string email)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var tokenData = new byte[32];
                rng.GetBytes(tokenData);
                var token = Convert.ToBase64String(tokenData);

                using (var db = new TFCEntities())
                {
                    var user = db.Usuarios.FirstOrDefault(u => u.email == email);
                    if (user != null)
                    {
                        user.VerificationToken = token;
                        db.SaveChanges();
                    }
                }

                return token;
            }
        }

        [AllowAnonymous]
        public ActionResult VerifyEmail(string token)
        {
            using (var db = new TFCEntities())
            {
                var user = db.Usuarios.FirstOrDefault(u => u.VerificationToken == token);
                if (user != null)
                {
                    user.verificado = true;
                    user.VerificationToken = null;
                    db.SaveChanges();
                    return RedirectToAction("VerificationSuccess");
                }

                return RedirectToAction("VerificationFailed");
            }
        }

        public ActionResult VerificationSuccess()
        {
            return View();
        }

        public ActionResult VerificationFailed()
        {
            return View();
        }

        public ActionResult Orders()
        {
            using (var db = new TFCEntities())
            {
                var userId = db.Usuarios.First(u => u.email == User.Identity.Name).id_usuario;
                var pedidos = db.Pedidos.Include(p => p.Estados).Include(p => p.Detalles_Pedidos).Where(p => p.id_usuario == userId).ToList();
                return View(pedidos);
            }
        }

        public ActionResult OrderDetails(string orderId)
        {
            using (var db = new TFCEntities())
            {
                var order = db.Pedidos.Include(o => o.Detalles_Pedidos).Include(o => o.Estados).Include(p => p.Actualizaciones_Pedidos).Include(p => p.Actualizaciones_Pedidos.Select(ap => ap.Estados)).FirstOrDefault(o => o.id_pedido == orderId) ?? throw new HttpException(404, "Página no encontrada");
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
                    }).ToList(),
                    OrderUpdates = order.Actualizaciones_Pedidos.ToList()
                };

                return View(orderDetails);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ChangePassword(string token)
        {
            if (string.IsNullOrEmpty(token) || !IsTokenValid(token))
                return RedirectToAction("InvalidToken");

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public JsonResult ChangePasswordSubmit(Usuarios model, string token)
        {
            if (string.IsNullOrEmpty(token) || !IsTokenValid(token))
                return Json(new { success = false, message = "Token inválido o expirado." });

            if (ModelState.IsValid)
            {
                using (var db = new TFCEntities())
                {
                    var user = db.Usuarios.SingleOrDefault(u => u.ResetPasswordToken == token);
                    if (user != null)
                    {
                        user.contrasenia = HashPassword(model.contrasenia);
                        user.ResetPasswordToken = null;
                        user.TokenExpiration = null;
                        db.SaveChanges();
                    }
                }

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Datos inválidos." });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> SendResetPasswordEmailAsync(string email)
        {
            using (var db = new TFCEntities())
            {
                bool isEmailValid = await VerifyEmailAsync(email);
                if (!isEmailValid)
                    return Json(new { success = false, message = "El correo electrónico no es válido." });

                var user = db.Usuarios.FirstOrDefault(u => u.email == email);
                if (user != null)
                {
                    var token = GenerateResetPasswordToken(email);
                    var callbackUrl = Url.Action("ChangePassword", "Account", new { token }, protocol: Request.Url.Scheme).ToLower();

                    var fromAddress = new MailAddress("solestocksolutions@gmail.com", "Sole Stock Solutions");
                    var toAddress = new MailAddress(email);
                    const string fromPassword = "mbbe uhmn vcaz ktbg";
                    string subject = "Restablecer contraseña";
                    string body = this.RenderViewToString("~/Views/Mails/ResetPassword.cshtml", callbackUrl);

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

                    return Json(new { success = true, message = "Correo de restablecimiento de contraseña enviado correctamente." });
                }
                else
                    return Json(new { success = false, message = "Correo electrónico no encontrado." });
            }
        }

        private bool IsTokenValid(string token)
        {
            using (var db = new TFCEntities())
            {
                var user = db.Usuarios.SingleOrDefault(u => u.ResetPasswordToken == token);
                if (user != null && user.TokenExpiration.HasValue)
                    return user.TokenExpiration.Value > DateTime.Now;

                return false;
            }
        }

        private void InvalidateToken(string token)
        {
            using (var db = new TFCEntities())
            {
                var user = db.Usuarios.SingleOrDefault(u => u.ResetPasswordToken == token);
                if (user != null)
                {
                    user.ResetPasswordToken = null;
                    user.TokenExpiration = null;
                    db.SaveChanges();
                }
            }
        }

        private string GenerateResetPasswordToken(string email)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var tokenData = new byte[32];
                rng.GetBytes(tokenData);
                var token = Convert.ToBase64String(tokenData);

                using (var db = new TFCEntities())
                {
                    var user = db.Usuarios.FirstOrDefault(u => u.email == email);
                    if (user != null)
                    {
                        user.ResetPasswordToken = token;
                        user.TokenExpiration = DateTime.Now.AddHours(1);
                        db.SaveChanges();
                    }
                }

                return token;
            }
        }

        [AllowAnonymous]
        public ActionResult InvalidToken()
        {
            return View();
        }
    }
}