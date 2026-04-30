using System;
using System.Collections.Generic;
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
        private Guid? GetClinicaId(ConexionModel db)
        {
            var userId = Guid.Parse(((ClaimsIdentity)User.Identity)
                                    .FindFirst(ClaimTypes.NameIdentifier).Value);
            var clinica = db.clinicas.FirstOrDefault(c => c.usuario_id == userId);
            return clinica?.id;
        }

        // ─── INDEX ───────────────────────────────────────────────────────────────
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
                ViewBag.Buscar = buscar;

                return View(query.OrderByDescending(r => r.creado_en).ToList());
            }
        }

        // ─── CREAR ───────────────────────────────────────────────────────────────
        public ActionResult Crear()
        {
            using (var db = new ConexionModel())
            {
                ViewBag.Categorias = new SelectList(
                    db.categorias.Where(c => c.padre_id == null).OrderBy(c => c.nombre).ToList(), 
                    "id", "nombre");
                return View(new solicitudes_rfq { estado = "abierta" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(solicitudes_rfq model)
        {
            // Limpiamos los campos que el servidor genera automáticamente
            ModelState.Remove("clinica_id");
            ModelState.Remove("estado");
            ModelState.Remove("creado_en");
            ModelState.Remove("actualizado_en");

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

                model.id = Guid.NewGuid();
                model.clinica_id = clinicaId.Value;
                model.estado = "abierta"; 
                model.creado_en = DateTime.Now;
                model.actualizado_en = DateTime.Now;

                db.solicitudes_rfq.Add(model);
                db.SaveChanges();

                TempData["Exito"] = "Solicitud enviada.";
                return RedirectToAction("Index");
            }
        }

        // ─── EDITAR ──────────────────────────────────────────────────────────────
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(solicitudes_rfq model)
        {
            // Para editar también removemos los campos que no vienen del form
            ModelState.Remove("clinica_id");
            ModelState.Remove("creado_en");
            ModelState.Remove("actualizado_en");

            if (!ModelState.IsValid)
            {
                using (var db = new ConexionModel())
                {
                    ViewBag.Categorias = new SelectList(
                        db.categorias.Where(c => c.padre_id == null).OrderBy(c => c.nombre).ToList(),
                        "id", "nombre", model.categoria_id);
                    ViewBag.Estados = new SelectList(new[]
                    {
                        new { Val = "abierta", Txt = "Abierta" },
                        new { Val = "en_revision", Txt = "En revisión" },
                        new { Val = "cerrada", Txt = "Cerrada" },
                        new { Val = "cancelada", Txt = "Cancelada" }
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

                // Actualizamos campos
                rfq.titulo = model.titulo;
                rfq.descripcion = model.descripcion;
                rfq.categoria_id = model.categoria_id;
                rfq.cantidad_estimada = model.cantidad_estimada;
                rfq.fecha_limite = model.fecha_limite;
                rfq.estado = model.estado;
                rfq.actualizado_en = DateTime.Now;

                db.SaveChanges();
                TempData["Exito"] = "Solicitud actualizada.";
                return RedirectToAction("Index");
            }
        }

        // ─── ELIMINAR Y DETALLES ─────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                var rfq = db.solicitudes_rfq.FirstOrDefault(r => r.id == id && r.clinica_id == clinicaId);
                if (rfq == null) return HttpNotFound();

                db.solicitudes_rfq.Remove(rfq);
                db.SaveChanges();
                TempData["Exito"] = "Solicitud eliminada.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult Detalles(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                var rfq = db.solicitudes_rfq
                            .Include("categorias")
                            .Include("cotizaciones.proveedores.usuarios") 
                            .FirstOrDefault(r => r.id == id && r.clinica_id == clinicaId);

                if (rfq == null) return HttpNotFound();
                return View(rfq);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(Guid id, string nuevoEstado)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                var rfq = db.solicitudes_rfq.FirstOrDefault(r => r.id == id && r.clinica_id == clinicaId);

                if (rfq == null) return HttpNotFound();

                // Actualizamos solo el estado y la fecha
                rfq.estado = nuevoEstado;
                rfq.actualizado_en = DateTime.Now;

                db.SaveChanges();

                TempData["Exito"] = "Estado de la solicitud actualizado.";
                return RedirectToAction("Detalles", new { id = rfq.id });
            }
        }

        // ─── ACEPTAR COTIZACIÓN ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AceptarCotizacion(Guid cotizacionId, Guid rfqId)
        {
            using (var db = new ConexionModel())
            {
                var clinicaId = GetClinicaId(db);
                // 1. Validar que la cotización y la RFQ existan y pertenezcan a la clínica
                var rfq = db.solicitudes_rfq.FirstOrDefault(r => r.id == rfqId && r.clinica_id == clinicaId);
                var cotizacion = db.cotizaciones.FirstOrDefault(c => c.id == cotizacionId && c.rfq_id == rfqId);

                if (cotizacion == null || rfq == null) return HttpNotFound();

                // 2. Cambiar los estados
                cotizacion.estado = "aceptada";
                rfq.estado = "cerrada";

                // Rechazar todas las demás cotizaciones de esta misma RFQ
                var otrasCotizaciones = db.cotizaciones.Where(c => c.rfq_id == rfqId && c.id != cotizacionId).ToList();
                foreach (var otra in otrasCotizaciones)
                {
                    otra.estado = "rechazada";
                }

                // 3. Crear notificación para el proveedor ganador
                var noti = new notificaciones_proveedores
                {
                    id = Guid.NewGuid(),
                    proveedor_id = cotizacion.proveedor_id,
                    titulo = "¡Cotización Aceptada!",
                    mensaje = $"Tu oferta para la solicitud '{rfq.titulo}' fue aceptada. Contacta a la clínica para coordinar.",
                    tipo = "rfq",
                    leida = false,
                    creado_en = DateTime.Now
                };
                db.notificaciones_proveedores.Add(noti);

                db.SaveChanges();
                
                TempData["Exito"] = "Cotización aceptada. La solicitud ha sido cerrada.";
                return RedirectToAction("Detalles", new { id = rfqId });
            }
        }
    }
}