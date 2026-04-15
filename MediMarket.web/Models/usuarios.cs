namespace MediMarket.web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class usuarios
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public usuarios()
        {
            clinicas = new HashSet<clinicas>();
            proveedores = new HashSet<proveedores>();
            suscripciones = new HashSet<suscripciones>();
        }

        public Guid id { get; set; }

        [Required]
        [StringLength(255)]
        public string email { get; set; }

        [StringLength(255)]
        public string nombre { get; set; }

        public string foto_url { get; set; }

        [Required]
        [StringLength(20)]
        public string tipo_usuario { get; set; }

        [Required]
        [StringLength(20)]
        public string estado_verificacion { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime creado_en { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<clinicas> clinicas { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<proveedores> proveedores { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<suscripciones> suscripciones { get; set; }
    }
}
