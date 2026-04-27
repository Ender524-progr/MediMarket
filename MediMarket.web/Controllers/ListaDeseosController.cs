using System;
using System.Linq;
using System.Web.Mvc;
using MediMarket.web.Models;
using System.Security.Claims;
using System.Data.Entity;

namespace MediMarket.web.Controllers
{
    [Authorize]
    public class ListaDeseosController : Controller
    {
        // ─── GET: ListaDeseos ────────────────────────────────────────────────
        public ActionResult Index()
        {
            using (var db = new ConexionModel())
            {
                var userId = Guid.Parse(((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value);
                var clinica = db.clinicas.FirstOrDefault(c => c.usuario_id == userId);

                if (clinica == null) return RedirectToAction("Login", "Account");

                // Traemos los productos guardados con sus fotos y el nombre del proveedor
                var deseos = db.lista_deseos
                    .Include(l => l.productos)
                    .Include(l => l.productos.producto_imagenes)
                    .Include(l => l.productos.proveedores)
                    .Where(l => l.clinica_id == clinica.id)
                    .OrderByDescending(l => l.fecha_agregado)
                    .ToList();

                return View(deseos);
            }
        }

        // ─── POST: Agregar (Llamado por JS fetch desde Details) ──────────────
        [HttpPost]
        public JsonResult Agregar(Guid productoId)
        {
            try
            {
                using (var db = new ConexionModel())
                {
                    var userId = Guid.Parse(((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value);
                    var clinica = db.clinicas.FirstOrDefault(c => c.usuario_id == userId);

                    if (clinica == null) return Json(new { ok = false, mensaje = "Solo las clínicas pueden guardar favoritos." });

                    // Verificamos que no lo haya guardado ya para no duplicar
                    var existe = db.lista_deseos.Any(l => l.clinica_id == clinica.id && l.producto_id == productoId);
                    if (!existe)
                    {
                        db.lista_deseos.Add(new lista_deseos
                        {
                            id = Guid.NewGuid(),
                            clinica_id = clinica.id,
                            producto_id = productoId,
                            fecha_agregado = DateTime.Now
                        });
                        db.SaveChanges();
                    }
                    return Json(new { ok = true, mensaje = "Guardado en tu Lista de Deseos 💖" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = "Error: " + ex.Message });
            }
        }

        // ─── POST: Eliminar ──────────────────────────────────────────────────
        [HttpPost]
        public ActionResult Eliminar(Guid id) // Este es el ID de la tabla lista_deseos
        {
            using (var db = new ConexionModel())
            {
                var deseo = db.lista_deseos.Find(id);
                if (deseo != null)
                {
                    db.lista_deseos.Remove(deseo);
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
        }
    }
}