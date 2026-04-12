namespace MediMarket.web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class productos
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public productos()
        {
            detalle_pedidos = new HashSet<detalle_pedidos>();
            lista_deseos = new HashSet<lista_deseos>();
            producto_imagenes = new HashSet<producto_imagenes>();
        }

        public Guid id { get; set; }

        public Guid proveedor_id { get; set; }

        public Guid? categoria_id { get; set; }

        [Required]
        [StringLength(255)]
        public string nombre { get; set; }

        public string descripcion { get; set; }

        [StringLength(100)]
        public string sku { get; set; }

        public decimal precio_unitario { get; set; }

        [Required]
        [StringLength(30)]
        public string unidad_medida { get; set; }

        public int stock_disponible { get; set; }

        public bool activo { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime creado_en { get; set; }

        public virtual categorias categorias { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<detalle_pedidos> detalle_pedidos { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<lista_deseos> lista_deseos { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<producto_imagenes> producto_imagenes { get; set; }

        public virtual proveedores proveedores { get; set; }
    }
}
