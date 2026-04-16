using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MediMarket.web.Models;
using System.Security.Claims;

namespace MediMarket.web.Controllers
{
    [Authorize]
    public class ShopController : Controller
    {
        // Modelo para pasar a la vista
        public class ShopIndexViewModel
        {
            public List<CategoriaConProductos> Categorias { get; set; }
            public string Busqueda { get; set; }
            public Guid? CategoriaFiltro { get; set; }
            public List<categorias> TodasCategorias { get; set; }
        }

        public class CategoriaConProductos
        {
            public categorias Categoria { get; set; }
            public List<productos> Productos { get; set; }
        }

        public ActionResult Index(string q = null, Guid? categoriaId = null)
        {
            using (var db = new ConexionModel())
            {
                // Base: solo productos activos con sus imágenes y proveedor
                var query = db.productos
                    .Include("producto_imagenes")
                    .Include("categorias")
                    .Include("proveedores")
                    .Where(p => p.activo);

                // Filtro de búsqueda
                if (!string.IsNullOrWhiteSpace(q))
                    query = query.Where(p => p.nombre.Contains(q) || p.descripcion.Contains(q));

                // Filtro por categoría
                if (categoriaId.HasValue)
                    query = query.Where(p => p.categoria_id == categoriaId.Value);

                var productos = query.OrderBy(p => p.nombre).ToList();

                // Agrupar por categoría
                var categorias = productos
                    .GroupBy(p => p.categorias)
                    .Select(g => new CategoriaConProductos
                    {
                        Categoria = g.Key, // puede ser null (sin categoría)
                        Productos = g.ToList()
                    })
                    .OrderBy(c => c.Categoria == null ? 1 : 0) // "Sin categoría" al final
                    .ThenBy(c => c.Categoria?.nombre)
                    .ToList();

                var vm = new ShopIndexViewModel
                {
                    Categorias = categorias,
                    Busqueda = q,
                    CategoriaFiltro = categoriaId,
                    TodasCategorias = db.categorias.OrderBy(c => c.nombre).ToList()
                };

                return View(vm);
            }
        }
    }
}