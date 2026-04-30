using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;
using IronPdf;

namespace MediMarket.web.Controllers
{
    public class OrdenesController : BaseProveedorController
    {
        private proveedores GetProveedorActual(ConexionModel db)
        {
            var uid = Guid.Parse(((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value);
            return db.proveedores.FirstOrDefault(p => p.usuario_id == uid);
        }

        // ── INDEX: Lista de pedidos ──────────────────────────────────────────
        public ActionResult Index(string estado = null)
        {
            using (var db = new ConexionModel())
            {
                var prov = GetProveedorActual(db);
                if (prov == null) return RedirectToAction("Login", "Account");

                // Traemos los pedidos que tengan AL MENOS UN detalle con mis productos
                var query = db.pedidos
                    .Include("clinicas")
                    .Include("detalle_pedidos.productos")
                    .Where(p => p.detalle_pedidos.Any(d => d.productos.proveedor_id == prov.id));

                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(p => p.estado == estado);
                }

                var pedidos = query.OrderByDescending(p => p.creado_en).ToList();
                ViewBag.FiltroEstado = estado;

                return View(pedidos);
            }
        }

        // ── DETAILS: Ver la orden y mis productos ────────────────────────────
        public ActionResult Details(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var prov = GetProveedorActual(db);

                // Buscamos el pedido asegurando que participamos en él
                var pedido = db.pedidos
                    .Include("clinicas")
                    .Include("detalle_pedidos.productos.producto_imagenes")
                    .FirstOrDefault(p => p.id == id && p.detalle_pedidos.Any(d => d.productos.proveedor_id == prov.id));

                if (pedido == null) return HttpNotFound("El pedido no existe o no te pertenece.");

                // EXTRAEMOS SOLO NUESTROS PRODUCTOS. 
                // Ignoramos si la clínica compró productos de otros proveedores en la misma orden.
                var misDetalles = pedido.detalle_pedidos
                    .Where(d => d.productos.proveedor_id == prov.id)
                    .ToList();

                ViewBag.MisDetalles = misDetalles;

                // Calculamos cuánto dinero de este pedido es nuestro realmente
                ViewBag.MiSubtotal = misDetalles.Sum(d => d.cantidad * d.precio_unitario);

                return View(pedido);
            }
        }

        // ── CAMBIAR ESTADO ───────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(Guid id, string nuevoEstado)
        {
            using (var db = new ConexionModel())
            {
                var prov = GetProveedorActual(db);
                var pedido = db.pedidos
                    .FirstOrDefault(p => p.id == id && p.detalle_pedidos.Any(d => d.productos.proveedor_id == prov.id));

                if (pedido != null)
                {
                    pedido.estado = nuevoEstado;
                    db.SaveChanges();
                    TempData["Exito"] = "El estado del pedido se actualizó a: " + nuevoEstado.Replace("_", " ").ToUpper();
                }

                return RedirectToAction("Details", new { id = id });
            }
        }
        // ── GENERAR PDF DE ENVÍO ─────────────────────────────────────────────
        [HttpGet]
        public ActionResult GenerarGuiaEnvio(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var prov = GetProveedorActual(db);
                if (prov == null) return new HttpUnauthorizedResult();

                // 1. Traemos la orden con la clínica y los detalles[cite: 23, 24]
                var pedido = db.pedidos
                    .Include("clinicas")
                    .Include("detalle_pedidos.productos")
                    .FirstOrDefault(p => p.id == id && p.detalle_pedidos.Any(d => d.productos.proveedor_id == prov.id));

                if (pedido == null) return HttpNotFound();

                // 2. Filtramos solo los productos de este proveedor
                var misDetalles = pedido.detalle_pedidos
                    .Where(d => d.productos.proveedor_id == prov.id)
                    .ToList();

                // 3. Armamos la plantilla HTML del PDF usando CSS básico
                string htmlContent = $@"
                <!DOCTYPE html>
                <html lang='es'>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #333; margin: 40px; }}
                        .header {{ border-bottom: 2px solid #0f766e; padding-bottom: 20px; margin-bottom: 30px; }}
                        .title {{ font-size: 28px; font-weight: bold; color: #0f766e; margin: 0; }}
                        .order-num {{ font-size: 14px; color: #666; margin-top: 5px; }}
                        .grid {{ display: table; width: 100%; margin-bottom: 30px; }}
                        .col {{ display: table-cell; width: 50%; }}
                        .box {{ background-color: #f8fafc; border: 1px solid #e2e8f0; padding: 15px; border-radius: 8px; margin-right: 15px; }}
                        .box-title {{ font-size: 12px; font-weight: bold; text-transform: uppercase; color: #64748b; margin-top: 0; margin-bottom: 10px; border-bottom: 1px solid #e2e8f0; padding-bottom: 5px; }}
                        .text-bold {{ font-weight: bold; color: #0f172a; font-size: 16px; margin: 0 0 5px 0; }}
                        .text-sm {{ font-size: 14px; color: #475569; margin: 2px 0; }}
                        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                        th {{ background-color: #f1f5f9; text-align: left; padding: 10px; font-size: 12px; color: #475569; text-transform: uppercase; border-bottom: 2px solid #cbd5e1; }}
                        td {{ padding: 12px 10px; border-bottom: 1px solid #e2e8f0; font-size: 14px; color: #334155; }}
                        .footer {{ margin-top: 50px; font-size: 12px; text-align: center; color: #94a3b8; border-top: 1px solid #e2e8f0; padding-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1 class='title'>Hoja de Remisión / Envío</h1>
                        <p class='order-num'>Pedido #<strong>{pedido.numero_pedido}</strong> | Fecha: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}</p>
                    </div>

                    <div class='grid'>
                        <div class='col'>
                            <div class='box'>
                                <h3 class='box-title'>Remitente (Proveedor)</h3>
                                <p class='text-bold'>{prov.nombre_empresa}</p>
                                <p class='text-sm'>RFC: {prov.rfc}</p>
                                <p class='text-sm'>{prov.direccion}</p>
                                <p class='text-sm'>CP {prov.cp}</p>
                                <p class='text-sm'>Tel: {prov.telefono}</p>
                            </div>
                        </div>
                        <div class='col'>
                            <div class='box' style='margin-right:0;'>
                                <h3 class='box-title'>Destinatario (Clínica)</h3>
                                <p class='text-bold'>{pedido.clinicas.nombre_clinica}</p>
                                <p class='text-sm'>Atención: {pedido.clinicas.especialidad}</p>
                                <p class='text-sm'>{pedido.clinicas.direccion}</p>
                                <p class='text-sm'>{pedido.clinicas.ciudad}, {pedido.clinicas.estado}. CP {pedido.clinicas.cp}</p>
                                <p class='text-sm'>Tel: {pedido.clinicas.telefono}</p>
                            </div>
                        </div>
                    </div>

                    <h3 style='font-size: 16px; color: #0f172a; margin-bottom: 10px;'>Contenido del Paquete</h3>
                    <table>
                        <thead>
                            <tr>
                                <th>Cantidad</th>
                                <th>SKU</th>
                                <th>Descripción del Producto</th>
                            </tr>
                        </thead>
                        <tbody>";

                // Agregamos las filas de la tabla dinámicamente
                foreach (var item in misDetalles)
                {
                    htmlContent += $@"
                            <tr>
                                <td style='font-weight:bold; text-align:center; width: 80px;'>{item.cantidad}x</td>
                                <td style='width: 120px;'>{item.productos.sku ?? "N/A"}</td>
                                <td>{item.productos.nombre}</td>
                            </tr>";
                }

                htmlContent += $@"
                        </tbody>
                    </table>

                    <div class='footer'>
                        Documento generado automáticamente por el sistema MediMarket. Incluya esta hoja dentro o fuera del paquete.
                    </div>
                </body>
                </html>";

                // 4. Inicializamos el renderizador de IronPDF
                var renderer = new ChromePdfRenderer();

                // (Opcional) Configuramos el papel
                renderer.RenderingOptions.PaperSize = IronPdf.Rendering.PdfPaperSize.Letter;
                renderer.RenderingOptions.MarginTop = 10;
                renderer.RenderingOptions.MarginBottom = 10;

                // 5. Convertimos el HTML a PDF
                var pdf = renderer.RenderHtmlAsPdf(htmlContent);

                // 6. Retornamos el archivo PDF al navegador para descarga
                return File(pdf.BinaryData, "application/pdf", $"Remision_Pedido_{pedido.numero_pedido}.pdf");
            }
        }
    }
}