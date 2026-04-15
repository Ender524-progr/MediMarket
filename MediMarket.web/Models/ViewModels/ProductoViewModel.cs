// Models/ViewModels/ProductoViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MediMarket.web.Models.ViewModels
{
    public class ProductoViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(255)]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        [StringLength(100)]
        public string Sku { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
        public decimal PrecioUnitario { get; set; }

        [Required(ErrorMessage = "La unidad de medida es obligatoria")]
        [StringLength(30)]
        public string UnidadMedida { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int StockDisponible { get; set; }

        public bool Activo { get; set; } = true;

        public Guid? CategoriaId { get; set; }

        // Para el dropdown de categorías en la vista
        public IEnumerable<categorias> Categorias { get; set; }

        // Imágenes existentes (para Edit)
        public IEnumerable<producto_imagenes> ImagenesActuales { get; set; }

        // Nuevas imágenes a subir
        public IEnumerable<HttpPostedFileBase> Imagenes { get; set; }
    }
}