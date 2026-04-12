namespace MediMarket.web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class suscripciones
    {
        public Guid id { get; set; }

        public Guid usuario_id { get; set; }

        public Guid plan_id { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime fecha_inicio { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? fecha_fin { get; set; }

        [Required]
        [StringLength(20)]
        public string estado { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime creado_en { get; set; }

        public virtual planes planes { get; set; }

        public virtual usuarios usuarios { get; set; }
    }
}
