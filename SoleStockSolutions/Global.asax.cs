using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace SoleStockSolutions
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var url = HttpContext.Current.Request.Url.ToString();
            var lowerUrl = url.ToLowerInvariant();

            if (url != lowerUrl)
            {
                HttpContext.Current.Response.RedirectPermanent(lowerUrl);
            }
        }
    }
}
