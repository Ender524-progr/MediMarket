namespace MediMarket.web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class pedidos
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public pedidos()
        {
            detalle_pedidos = new HashSet<detalle_pedidos>();
        }

        public Guid id { get; set; }

        public Guid clinica_id { get; set; }

        [Required]
        [StringLength(50)]
        public string numero_pedido { get; set; }

        [Required]
        [StringLength(20)]
        public string estado { get; set; }

        public decimal total { get; set; }

        [StringLength(50)]
        public string metodo_pago { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime creado_en { get; set; }

        public virtual clinicas clinicas { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<detalle_pedidos> detalle_pedidos { get; set; }
    }
}
