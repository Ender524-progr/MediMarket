namespace MediMarket.web.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Collections.Generic;

    public partial class solicitudes_rfq
    {
        public Guid id { get; set; }

        public Guid clinica_id { get; set; }

        public Guid? categoria_id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(255)]
        public string titulo { get; set; }

        public string descripcion { get; set; }

        [StringLength(100)]
        public string cantidad_estimada { get; set; }

        [Column(TypeName = "date")]
        public DateTime? fecha_limite { get; set; }

        [Required]
        [StringLength(20)]
        public string estado { get; set; }

        [Column(TypeName = "datetime2")]    
        public DateTime creado_en { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime actualizado_en { get; set; }

        // Navegación
        public virtual clinicas    clinicas   { get; set; }
        public virtual categorias  categorias { get; set; }
        public virtual ICollection<cotizaciones> cotizaciones { get; set; }
    }
}
