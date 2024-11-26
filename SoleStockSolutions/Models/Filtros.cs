using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace SoleStockSolutions.Models
{
    public class FiltroMarcas
    {
        public int Id_Marca { get; set; }
        public string Nombre_Marca { get; set; }
        public int CantidadProductos { get; set; }
    }

    public class FiltroModelos
    {
        public int Id_Modelo { get; set; }
        public string Nombre_Modelo { get; set; }
        public int CantidadProductos { get; set; }
    }

    public class FiltroTallas
    {
        public string Talla { get; set; }
        public int CantidadProductos { get; set; }
    }

    public class FiltroPrecios
    {
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
    }
}