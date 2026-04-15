using System;
using System.Linq;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;
using System.Data.Entity;

namespace MediMarket.web.Controllers
{
    public class SolicitudesRfqController : BaseProveedorController
    {
        // ─── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Obtiene el clinica_id del usuario autenticado (o null si no es clínica).</summary>
        private Guid? GetClinicaId(ConexionModel db)
        {
            var userId = Guid.Parse(((ClaimsIdentity)User.Identity)
                                    .FindFirst(ClaimTypes.NameIdentifier).Value);

            var clinica = db.clinicas.FirstOrDefault(c => c.usuario_id == userId);
            return clinica?.id;
        }

        // ─── INDEX ───────────────────────────────────────────────────────────────
        // GET: /SolicitudesRfq
        public ActionResult Index(string estado, string buscar)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                if (clinicaId == null) return RedirectToAction("Index", "Proveedores");

                var query = db.solicitudes_rfq
                              .Include("categorias")
                              .Where(r => r.clinica_id == clinicaId.Value);

                if (!string.IsNullOrWhiteSpace(estado))
                    query = query.Where(r => r.estado == estado);

                if (!string.IsNullOrWhiteSpace(buscar))
                    query = query.Where(r => r.titulo.Contains(buscar));

                ViewBag.EstadoFiltro = estado;
                ViewBag.Buscar       = buscar;

                return View(query.OrderByDescending(r => r.creado_en).ToList());
            }
        }

        // CREAR 
public ActionResult Crear()
{
    using (var db = new ConexionModel())
    {
        // AGREGA EL .ToList() AL FINAL AQUÍ:
        ViewBag.Categorias = new SelectList(
            db.categorias.Where(c => c.padre_id == null).OrderBy(c => c.nombre).ToList(), 
            "id", "nombre");

        return View(new solicitudes_rfq { estado = "abierta" });
    }
}

        // POST: /SolicitudesRfq/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(solicitudes_rfq model)
        {
            if (!ModelState.IsValid)
            {
                using (var db = new ConexionModel())
                {
                    ViewBag.Categorias = new SelectList(
                        db.categorias.Where(c => c.padre_id == null).OrderBy(c => c.nombre).ToList(),
                        "id", "nombre", model.categoria_id);
                    return View(model);
                }
            }

            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                if (clinicaId == null) return RedirectToAction("Index", "Proveedores");

                model.id             = Guid.NewGuid();
                model.clinica_id     = clinicaId.Value;
                model.estado         = "abierta";
                model.creado_en      = DateTime.Now;
                model.actualizado_en = DateTime.Now;

                db.solicitudes_rfq.Add(model);
                db.SaveChanges();

                TempData["Exito"] = "Solicitud publicada correctamente.";
                return RedirectToAction("Index");
            }
        }

        // ─── EDITAR 
        // GET
        public ActionResult Editar(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                var rfq = db.solicitudes_rfq
                            .FirstOrDefault(r => r.id == id && r.clinica_id == clinicaId);

                if (rfq == null) return HttpNotFound();

                ViewBag.Categorias = new SelectList(
                    db.categorias.Where(c => c.padre_id == null).OrderBy(c => c.nombre).ToList(),
                    "id", "nombre", rfq.categoria_id);

                ViewBag.Estados = new SelectList(new[]
                {
                    new { Val = "abierta",     Txt = "Abierta"     },
                    new { Val = "en_revision", Txt = "En revisión" },
                    new { Val = "cerrada",     Txt = "Cerrada"     },
                    new { Val = "cancelada",   Txt = "Cancelada"   }
                }, "Val", "Txt", rfq.estado);

                return View(rfq);
            }
        }

        // POST: Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(solicitudes_rfq model)
        {
            if (!ModelState.IsValid)
            {
                using (var db = new ConexionModel())
                {
                    ViewBag.Categorias = new SelectList(
                        db.categorias.Where(c => c.padre_id == null).OrderBy(c => c.nombre).ToList(),
                        "id", "nombre", model.categoria_id);
                    ViewBag.Estados = new SelectList(new[]
                    {
                        new { Val = "abierta",     Txt = "Abierta"     },
                        new { Val = "en_revision", Txt = "En revisión" },
                        new { Val = "cerrada",     Txt = "Cerrada"     },
                        new { Val = "cancelada",   Txt = "Cancelada"   }
                    }, "Val", "Txt", model.estado);
                    return View(model);
                }
            }

            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                var rfq = db.solicitudes_rfq
                            .FirstOrDefault(r => r.id == model.id && r.clinica_id == clinicaId);

                if (rfq == null) return HttpNotFound();

                rfq.titulo            = model.titulo;
                rfq.descripcion       = model.descripcion;
                rfq.categoria_id      = model.categoria_id;
                rfq.cantidad_estimada = model.cantidad_estimada;
                rfq.fecha_limite      = model.fecha_limite;
                rfq.estado            = model.estado;
                rfq.actualizado_en    = DateTime.Now;

                db.SaveChanges();

                TempData["Exito"] = "Solicitud actualizada.";
                return RedirectToAction("Index");
            }
        }

        // ELIMINAR 
        // POST: Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                var rfq = db.solicitudes_rfq
                            .FirstOrDefault(r => r.id == id && r.clinica_id == clinicaId);

                if (rfq == null) return HttpNotFound();

                db.solicitudes_rfq.Remove(rfq);
                db.SaveChanges();

                TempData["Exito"] = "Solicitud eliminada.";
                return RedirectToAction("Index");
            }
        }
        // GET: Detalles
        public ActionResult Detalles(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
        
                 // Buscamos la solicitud y cargamos sus cotizaciones y los datos del proveedor
                var rfq = db.solicitudes_rfq
                    .Include("categorias")
                    .Include("cotizaciones.proveedores.usuarios") 
                    .FirstOrDefault(r => r.id == id && r.clinica_id == clinicaId);

                if (rfq == null) return HttpNotFound();

                return View(rfq);
            }
        }
    }
}
