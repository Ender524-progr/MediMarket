namespace MediMarket.web.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class notificaciones_proveedores
    {
        public Guid id { get; set; }

        public Guid proveedor_id { get; set; }

        [Required]
        [StringLength(100)]
        public string titulo { get; set; }

        [Required]
        [StringLength(255)]
        public string mensaje { get; set; }

        [Required]
        [StringLength(50)]
        public string tipo { get; set; }

        public bool leida { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime creado_en { get; set; }

        public virtual proveedores proveedores { get; set; }
    }
}