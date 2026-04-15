namespace MediMarket.web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class planes
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public planes()
        {
            suscripciones = new HashSet<suscripciones>();
        }

        public Guid id { get; set; }

        [Required]
        [StringLength(100)]
        public string nombre { get; set; }

        public decimal precio_mensual { get; set; }

        public int? limite_productos { get; set; }

        public bool activo { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<suscripciones> suscripciones { get; set; }
    }
}
