namespace MediMarket.web.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class producto_imagenes
    {
        public Guid id { get; set; }

        public Guid producto_id { get; set; }

        [Required]
        public string url { get; set; }

        public bool es_principal { get; set; }

        public int orden { get; set; }

        public virtual productos productos { get; set; }
    }
}
