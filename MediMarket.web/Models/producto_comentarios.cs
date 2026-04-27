using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediMarket.web.Models
{
    public partial class producto_comentarios
    {
        [Key]
        public Guid id { get; set; }

        public Guid producto_id { get; set; }

        public Guid clinica_id { get; set; }

        public int calificacion { get; set; }

        [Required]
        public string comentario { get; set; }

        public DateTime creado_en { get; set; }

        public DateTime actualizado_en { get; set; }

        // Relaciones exactas con tus tablas
        [ForeignKey("producto_id")]
        public virtual productos productos { get; set; }

        [ForeignKey("clinica_id")]
        public virtual clinicas clinicas { get; set; }
    }
}