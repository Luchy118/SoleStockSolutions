using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new RemoveTrailingSlashFilter());
            filters.Add(new RemoveIndexActionFilter());
        }

    }
}
