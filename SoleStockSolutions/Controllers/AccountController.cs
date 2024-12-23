using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    public class AccountController : Controller
    {
        /// <summary>
        /// Muestra la vista de inicio de sesión.
        /// </summary>
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index");

            var returnUrl = TempData["ReturnUrl"] as string;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CurrentAction = "LogIn";
            return View();
        }

        /// <summary>
        /// Muestra la vista de registro.
        /// </summary>
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
            ViewBag.CurrentAction = "Wishlist";
            return View();
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
        public JsonResult ValidateLogin(Usuarios u, string returnUrl)
        {
            using (var db = new TFCEntities())
            {
                Usuarios existe = db.Usuarios.FirstOrDefault(x => x.email == u.email);

                if (existe != null)
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
                else
                    return Json(new { success = false, message = "No existe ninguna cuenta registrada con ese nombre de usuario o correo electrónico." }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Valida y registra un nuevo usuario.
        /// </summary>
        /// <param name="u">Objeto Usuarios con los datos del nuevo usuario.</param>
        /// <returns>Resultado de la validación en formato JSON.</returns>
        public JsonResult ValidateSignUp(Usuarios u)
        {
            using (var db = new TFCEntities())
            {
                Usuarios existe = db.Usuarios.FirstOrDefault(x => x.email == u.email);

                if (existe != null)
                    return Json(new { success = false, message = "El correo electrónico ya está en uso." });
                else
                {
                    u.contrasenia = HashPassword(u.contrasenia);
                    u.fecha_registro = DateTime.Now;

                    db.Usuarios.Add(u);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Te has registrado correctamente, ya puedes iniciar sesión." }, JsonRequestBehavior.AllowGet);
                }
            }
        }

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

            if (roles.Count == 0)
                roles.Add("User");

            return string.Join(",", roles);
        }
    }
}