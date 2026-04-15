namespace MediMarket.web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class detalle_pedidos
    {
        public Guid id { get; set; }

        public Guid pedido_id { get; set; }

        public Guid producto_id { get; set; }

        public int cantidad { get; set; }

        public decimal precio_unitario { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? subtotal { get; set; }

        public virtual pedidos pedidos { get; set; }

        public virtual productos productos { get; set; }
    }
}
