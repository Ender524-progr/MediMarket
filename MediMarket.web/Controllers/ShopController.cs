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
// ─── DETAILS ─────────────────────────────────────────────────────────────
        public ActionResult Details(Guid id)
{
    using (var db = new ConexionModel())
    {
        ViewBag.EnListaDeseos = false; // Por defecto

        if (Request.IsAuthenticated)
        {
            var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr != null)
            {
                var uId = Guid.Parse(userIdStr);
                // OJO AQUÍ: Asegúrate de que esta consulta sí encuentre tu clínica
                var miClinica = db.clinicas.FirstOrDefault(c => c.usuario_id == uId);
                
                if (miClinica != null)
                {
                    // DEBUG: Manda el ID a la vista para que verifiques que sea el correcto
                    ViewBag.MiClinicaDebug = miClinica.id; 
                    
                    // Buscamos si existe el registro
                    bool existe = db.lista_deseos.Any(l => l.producto_id == id && l.clinica_id == miClinica.id);
                    ViewBag.EnListaDeseos = existe;
                }
            }
        }

                // 2. Traemos el producto con sus comentarios reales
                var producto = db.productos
                    .Include("categorias")
                    .Include("proveedores")
                    .Include("producto_imagenes")
                    .Include("producto_comentarios.clinicas") // <- Magia: Trae la info de quien comentó
                    .FirstOrDefault(p => p.id == id && p.activo);

                if (producto == null) return HttpNotFound("El producto no existe o ya no está disponible.");

                return View(producto);
            }
        }

        // ─── GUARDAR / EDITAR COMENTARIO ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarComentario(Guid producto_id, int calificacion, string comentario)
        {
            using (var db = new ConexionModel())
            {
                var userId = Guid.Parse(((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value);
                var clinica = db.clinicas.FirstOrDefault(c => c.usuario_id == userId);

                if (clinica == null) return HttpNotFound("Debes tener un perfil de clínica para comentar.");

                // Buscamos si esta clínica ya había opinado sobre este producto
                var opinionExistente = db.producto_comentarios
                    .FirstOrDefault(c => c.producto_id == producto_id && c.clinica_id == clinica.id);

                if (opinionExistente != null)
                {
                    // Si ya existe, lo actualizamos (Editar)
                    opinionExistente.calificacion = calificacion;
                    opinionExistente.comentario = comentario;
                    opinionExistente.actualizado_en = DateTime.Now;
                }
                else
                {
                    // Si no existe, lo creamos nuevo
                    db.producto_comentarios.Add(new producto_comentarios
                    {
                        id = Guid.NewGuid(),
                        producto_id = producto_id,
                        clinica_id = clinica.id,
                        calificacion = calificacion,
                        comentario = comentario,
                        creado_en = DateTime.Now,
                        actualizado_en = DateTime.Now
                    });
                }

                db.SaveChanges();
                TempData["Exito"] = "Tu opinión se ha guardado correctamente.";
                return RedirectToAction("Details", new { id = producto_id });
            }
        }
    }
}