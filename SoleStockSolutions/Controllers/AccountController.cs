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
        private static TFCEntities db = new TFCEntities();

        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index");

            var returnUrl = TempData["ReturnUrl"] as string;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CurrentAction = "LogIn";
            return View();
        }

        public ActionResult SignUp()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index");

            ViewBag.CurrentAction = "SignUp";
            return View();
        }

        public ActionResult Index()
        {
            ViewBag.CurrentAction = "AccountIndex";
            return View();
        }

        public ActionResult Wishlist()
        {
            ViewBag.CurrentAction = "Wishlist";
            return View();
        }

        [HttpGet]
        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public JsonResult ValidateLogin(Usuarios u, string returnUrl)
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

        public JsonResult ValidateSignUp(Usuarios u)
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

        public bool VerifyPassword(string inputPassword, string storedHash)
        {
            string inputHash = HashPassword(inputPassword);
            return inputHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }

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