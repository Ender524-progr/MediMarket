namespace MediMarket.web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class proveedores
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public proveedores()
        {
            productos = new HashSet<productos>();
            notificaciones_proveedores = new HashSet<notificaciones_proveedores>();
        }

        public Guid id { get; set; }

        public Guid usuario_id { get; set; }

        [Required]
        [StringLength(255)]
        public string nombre_empresa { get; set; }


        [StringLength(13)]
        public string rfc { get; set; }

        [StringLength(100)]
        public string categoria_principal { get; set; }

        [StringLength(20)]
        public string telefono { get; set; }

        [StringLength(10)]
        public string cp { get; set; }

        public string direccion { get; set; }

        [StringLength(50)]
        public string registro_sanitario { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<productos> productos { get; set; }

        public virtual usuarios usuarios { get; set; }

        /// <summary>
        /// Gets or sets the URL of the logo image associated with this entity.
        /// </summary>
        public string logo_url { get; set; }
        public string banner_url { get; set; }
        [StringLength(255)]
        public string eslogan { get; set; }

        /// <summary>
        /// Obtiene o establece un valor que indica si se debe notificar la solicitud de cotización (RFQ).
        /// </summary>
        public bool notificar_rfq { get; set; }
        public bool notificar_ordenes { get; set; }
        public bool notificar_inventario { get; set; }

        /// <summary>
        /// Obtiene o establece la clave bancaria estandarizada (CLABE) asociada a la cuenta.
        /// </summary>
        /// <remarks>La CLABE es un número de 18 dígitos utilizado en México para identificar cuentas
        /// bancarias y facilitar transferencias electrónicas. El valor debe cumplir con el formato y longitud
        /// requeridos por las instituciones financieras mexicanas.</remarks>
        [StringLength(18)]
        public string cuenta_clabe { get; set; }
        [StringLength(50)]
        public string banco { get; set; }
        [StringLength(100)]
        public string regimen_fiscal { get; set; }

        /// <summary>
        /// Obtiene o establece el número de días requeridos para la preparación.
        /// </summary>
        public int dias_preparacion { get; set; }
        public decimal? monto_envio_gratis { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<notificaciones_proveedores> notificaciones_proveedores { get; set; }
    }
}
