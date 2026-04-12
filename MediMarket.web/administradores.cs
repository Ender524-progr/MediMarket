namespace MediMarket.web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class administradores
    {
        public Guid id { get; set; }

        [Required]
        [StringLength(255)]
        public string email { get; set; }

        [StringLength(255)]
        public string nombre { get; set; }

        [StringLength(100)]
        public string google_sub { get; set; }

        [Required]
        [StringLength(30)]
        public string rol { get; set; }

        public bool activo { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime creado_en { get; set; }
    }
}
