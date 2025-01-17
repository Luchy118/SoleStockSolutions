using System.Linq;
using System.Web.Mvc;
using SoleStockSolutions.Models;

namespace SoleStockSolutions.Filters
{
    public class LoadModelosRelevantesAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            using (var db = new TFCEntities())
            {
                var modelos = db.Modelos
                    .Select(m => new
                    {
                        modelo = m,
                        vistas_ventas = m.Productos.Sum(p => p.veces_visto) + m.Productos.SelectMany(p => p.Inventario).Sum(i => i.veces_vendido)
                    })
                    .Where(m => m.vistas_ventas > 0)
                    .OrderBy(m => m.modelo.id_marca)
                    .ThenByDescending(m => m.vistas_ventas)
                    .Take(4)
                    .ToList();
                var modelosRelevantes = modelos.Select(m => m.modelo).ToList();

                filterContext.Controller.ViewBag.ModelosRelevantes = modelosRelevantes;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
