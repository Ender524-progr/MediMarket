namespace MediMarket.web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class clinicas
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public clinicas()
        {
            lista_deseos = new HashSet<lista_deseos>();
            pedidos = new HashSet<pedidos>();
        }

        public Guid id { get; set; }

        public Guid usuario_id { get; set; }

        [Required]
        [StringLength(255)]
        public string nombre_clinica { get; set; }

        [StringLength(13)]
        public string rfc { get; set; }

        [StringLength(100)]
        public string especialidad { get; set; }

        [StringLength(20)]
        public string telefono { get; set; }

        [StringLength(10)]
        public string cp { get; set; }

        public string direccion { get; set; }

        [StringLength(100)]
        public string ciudad { get; set; }

        [StringLength(60)]
        public string estado { get; set; }

        [StringLength(20)]
        public string cedula_profesional { get; set; }

        public virtual usuarios usuarios { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<lista_deseos> lista_deseos { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<pedidos> pedidos { get; set; }
    }
}
