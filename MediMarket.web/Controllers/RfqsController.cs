using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;

namespace MediMarket.web.Controllers
{
    public class RFQsController : BaseProveedorController
    {
        // ViewModel
        public class RFQsIndexViewModel
        {
            public List<solicitudes_rfq> Solicitudes { get; set; }
            public List<categorias> Categorias { get; set; }
            public string FiltroBusqueda { get; set; }
            public Guid? FiltroCategoria { get; set; }
            public string FiltroEstado { get; set; }
        }

        private proveedores GetProveedorActual(ConexionModel db)
        {
            var uid = Guid.Parse(
                ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value
            );
            return db.proveedores.FirstOrDefault(p => p.usuario_id == uid);
        }

        // ── INDEX ────────────────────────────────────────────────────────────
        public ActionResult Index(string q = null, Guid? categoriaId = null, string estado = null)
        {
            using (var db = new ConexionModel())
            {
                var query = db.solicitudes_rfq
                    .Include("clinicas")
                    .Include("categorias")
                    .Include("cotizaciones")
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(q))
                    query = query.Where(r => r.titulo.Contains(q) || r.descripcion.Contains(q));

                if (categoriaId.HasValue)
                    query = query.Where(r => r.categoria_id == categoriaId.Value);

                if (!string.IsNullOrWhiteSpace(estado))
                    query = query.Where(r => r.estado == estado);

                var vm = new RFQsIndexViewModel
                {
                    Solicitudes = query.OrderByDescending(r => r.creado_en).ToList(),
                    Categorias = db.categorias.OrderBy(c => c.nombre).ToList(),
                    FiltroBusqueda = q,
                    FiltroCategoria = categoriaId,
                    FiltroEstado = estado
                };

                return View(vm);
            }
        }

        // ── DETAILS ──────────────────────────────────────────────────────────
        public ActionResult Details(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var rfq = db.solicitudes_rfq
                    .Include("clinicas")
                    .Include("categorias")
                    .Include("cotizaciones")
                    .Include("cotizaciones.proveedores")
                    .FirstOrDefault(r => r.id == id);

                if (rfq == null) return HttpNotFound();

                // Cotización del proveedor actual (si ya respondió)
                var proveedor = GetProveedorActual(db);
                ViewBag.MiCotizacion = rfq.cotizaciones
                    .FirstOrDefault(c => c.proveedor_id == proveedor?.id);
                ViewBag.ProveedorId = proveedor?.id;

                return View(rfq);
            }
        }

        // ── COTIZAR (POST) ────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cotizar(Guid rfqId, decimal monto, string tiempoEntrega, string notas)
        {
            using (var db = new ConexionModel())
            {
                var proveedor = GetProveedorActual(db);
                if (proveedor == null) return RedirectToAction("Login", "Account");

                // Evitar duplicados — actualizar si ya cotizó
                var existente = db.cotizaciones
                    .FirstOrDefault(c => c.rfq_id == rfqId && c.proveedor_id == proveedor.id);

                if (existente != null)
                {
                    existente.monto = monto;
                    existente.tiempo_entrega = tiempoEntrega;
                    existente.notas = notas;
                    existente.estado = "pendiente";
                }
                else
                {
                    db.cotizaciones.Add(new cotizaciones
                    {
                        id = Guid.NewGuid(),
                        rfq_id = rfqId,
                        proveedor_id = proveedor.id,
                        monto = monto,
                        tiempo_entrega = tiempoEntrega,
                        notas = notas,
                        estado = "pendiente",
                        creado_en = DateTime.Now
                    });
                }

                db.SaveChanges();
                TempData["Exito"] = "Cotización enviada correctamente.";
                return RedirectToAction("Details", new { id = rfqId });
            }
        }
    }
}