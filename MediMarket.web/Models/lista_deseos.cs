namespace MediMarket.web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class lista_deseos
    {
        public Guid id { get; set; }

        public Guid clinica_id { get; set; }

        public Guid producto_id { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime fecha_agregado { get; set; }

        public virtual clinicas clinicas { get; set; }

        public virtual productos productos { get; set; }
    }
}
