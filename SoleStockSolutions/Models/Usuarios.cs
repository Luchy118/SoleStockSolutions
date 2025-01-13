//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SoleStockSolutions.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Usuarios
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Usuarios()
        {
            this.Wishlist = new HashSet<Wishlist>();
            this.Direcciones = new HashSet<Direcciones>();
            this.Pedidos = new HashSet<Pedidos>();
        }
    
        public int id_usuario { get; set; }
        public string nombre { get; set; }
        public string apellidos { get; set; }
        public string email { get; set; }
        public string contrasenia { get; set; }
        public bool verificado { get; set; }
        public bool administrador { get; set; }
        public bool super_administrador { get; set; }
        public Nullable<System.DateTime> fecha_registro { get; set; }
        public bool guest { get; set; }
        public string VerificationToken { get; set; }
        public bool baja { get; set; }
        public Nullable<System.DateTime> fecha_baja { get; set; }
        public string ResetPasswordToken { get; set; }
        public Nullable<System.DateTime> TokenExpiration { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Wishlist> Wishlist { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Direcciones> Direcciones { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Pedidos> Pedidos { get; set; }
    }
}
