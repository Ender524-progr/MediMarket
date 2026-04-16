using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediMarket.web.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public partial class cotizaciones
    {
        public Guid id { get; set; }
        public Guid rfq_id { get; set; }
        public Guid proveedor_id { get; set; }

        [Required]
        public decimal monto { get; set; }
        public string tiempo_entrega { get; set; }
        public string notas { get; set; }
        public string estado { get; set; }
        public DateTime creado_en { get; set; }

        // Navegación
       // Le decimos a EF: "Oye, para la tabla solicitudes_rfq, usa la columna rfq_id"
        [ForeignKey("rfq_id")]
        public virtual solicitudes_rfq solicitudes_rfq { get; set; }

        // Le decimos a EF: "Para la tabla proveedores, usa la columna proveedor_id"
        [ForeignKey("proveedor_id")]
        public virtual proveedores proveedores { get; set; }
    }
}