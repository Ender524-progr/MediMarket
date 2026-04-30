using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediMarket.web.Models
{
    public class notificaciones_clinicas
    {
        [Key]
        public Guid id { get; set; }

        public Guid clinica_id { get; set; }

        public string titulo { get; set; }
        public string mensaje { get; set; }
        public string tipo { get; set; }
        public bool leida { get; set; }
        public DateTime creado_en { get; set; }

        // Relación para que puedas acceder a los datos de la clínica si los ocupas
        [ForeignKey("clinica_id")]
        public virtual clinicas clinicas { get; set; }
    }
}