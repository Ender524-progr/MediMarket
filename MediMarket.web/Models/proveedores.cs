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
    }
}
