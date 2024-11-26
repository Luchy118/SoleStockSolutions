using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SoleStockSolutions
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "Root",
                url: "",
                defaults: new { controller = "Home", action = "Index" },
                namespaces: new[] { "SoleStockSolutions.Controllers" }
            );

            routes.MapRoute(
                name: "ProductDetails",
                url: "products/{productName}",
                defaults: new { controller = "Products", action = "ProductDetails" },
                constraints: new { productName = new ExcludeActionNamesConstraint("GetMinMaxPrice", "GetAllModels", "GetModelsByBrands", "FilterProducts") },
                namespaces: new[] { "SoleStockSolutions.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "SoleStockSolutions.Controllers" }
            ).RouteHandler = new LowercaseRouteHandler();
        }
    }

    public class LowercaseRouteHandler : MvcRouteHandler
    {
        protected override IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            var values = requestContext.RouteData.Values;

            if (values.ContainsKey("controller"))
            {
                values["controller"] = values["controller"].ToString().ToLowerInvariant();
            }

            if (values.ContainsKey("action"))
            {
                values["action"] = values["action"].ToString().ToLowerInvariant();
            }

            if (values.ContainsKey("id") && values["id"] != null)
            {
                values["id"] = values["id"].ToString().ToLowerInvariant();
            }

            return base.GetHttpHandler(requestContext);
        }
    }

    public class RemoveTrailingSlashFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var response = filterContext.HttpContext.Response;

            if (request.Url != null && (request.Url.AbsolutePath.EndsWith("/") || request.Url.AbsolutePath.EndsWith("#")) && request.Url.AbsolutePath.Length > 1)
            {
                var newUrl = request.Url.AbsolutePath.TrimEnd('/', '#');
                if (!string.IsNullOrEmpty(request.Url.Query))
                {
                    newUrl += request.Url.Query;
                }

                response.RedirectPermanent(newUrl, true);
            }

            base.OnActionExecuting(filterContext);
        }
    }

    public class RemoveIndexActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var response = filterContext.HttpContext.Response;

            if (request.Url != null && request.Url.AbsolutePath.ToLowerInvariant().EndsWith("/index"))
            {
                var newUrl = request.Url.AbsolutePath.Substring(0, request.Url.AbsolutePath.Length - 6);
                if (!string.IsNullOrEmpty(request.Url.Query))
                {
                    newUrl += request.Url.Query;
                }

                response.RedirectPermanent(newUrl, true);
            }

            base.OnActionExecuting(filterContext);
        }
    }

    public class ExcludeActionNamesConstraint : IRouteConstraint
    {
        private readonly string[] _excludedNames;

        public ExcludeActionNamesConstraint(params string[] excludedNames)
        {
            _excludedNames = excludedNames;
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values[parameterName] == null) return false;

            string parameterValue = values[parameterName].ToString();
            return !_excludedNames.Contains(parameterValue, StringComparer.OrdinalIgnoreCase);
        }
    }
}
