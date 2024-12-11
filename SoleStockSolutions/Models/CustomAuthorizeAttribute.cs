using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace SoleStockSolutions.Models
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var email = httpContext.User.Identity.Name;
            var rol = GetRoleForUser(email);

            if (Roles.Split(',').Any(r => r == rol))
            {
                return true;
            }

            return false;
        }

        private string GetRoleForUser(string email)
        {
            using (var db = new TFCEntities())
            {
                var user = db.Usuarios.FirstOrDefault(u => u.email == email);
                if (user != null)
                {
                    if (user.super_administrador)
                        return "SuperAdmin";
                    if (user.administrador)
                        return "Admin";
                }
                return "User";
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "Home", action = "Unauthorized" }
                    )
                );
            }
            else
            {
                var returnUrl = filterContext.HttpContext.Request.Url?.PathAndQuery;
                filterContext.Controller.TempData["ReturnUrl"] = returnUrl;
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "Account", action = "Login" }
                    )
                );
            }
        }
    }
}