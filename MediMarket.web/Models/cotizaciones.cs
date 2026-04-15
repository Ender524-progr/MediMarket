using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
        public virtual solicitudes_rfq solicitudes_rfq { get; set; }
        public virtual proveedores proveedores { get; set; }
    }
}