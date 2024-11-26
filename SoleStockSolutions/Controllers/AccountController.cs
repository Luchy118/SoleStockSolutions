using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using SoleStockSolutions.Models;

namespace SoleStockSolutions.Controllers
{
    public class AccountController : Controller
    {
        private static TFCEntities db = new TFCEntities();

        public ActionResult Login()
        {
            ViewBag.CurrentAction = "LogIn";
            return View();
        }

        public ActionResult SignUp()
        {
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
            Session["user"] = null;
            return RedirectToAction("Index", "Home");
        }

        public JsonResult ValidateLogin(Usuarios u)
        {
            Usuarios existe = db.Usuarios.FirstOrDefault(x => x.email == u.email);

            if (existe != null)
            {
                if (VerifyPassword(u.contrasenia, existe.contrasenia))
                {
                    Session["user"] = existe.email;
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
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
    }
}