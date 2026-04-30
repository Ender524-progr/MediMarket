using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;
using IronPdf;

namespace MediMarket.web.Controllers
{
    public class AnalisisController : BaseProveedorController
    {
        // ─── MODELOS PARA EL ANÁLISIS ─────────────────────────────────────────
        public class AnalisisViewModel
        {
            public decimal TotalIngresos { get; set; }
            public int TotalArticulosVendidos { get; set; }
            public List<ProductoVentas> TopProductos { get; set; }
            public List<CategoriaVentas> VentasCategoria { get; set; }
            public List<VentaMensual> VentasPorMes { get; set; } // <--- NUEVO
        }

        public class ProductoVentas
        {
            public string Nombre { get; set; }
            public string Sku { get; set; }
            public int CantidadVendida { get; set; }
            public decimal IngresosGenerados { get; set; }
        }

        public class CategoriaVentas
        {
            public string Categoria { get; set; }
            public decimal Total { get; set; }
        }

        public class VentaMensual // <--- NUEVO
        {
            public string Mes { get; set; }
            public decimal Total { get; set; }
            public int Articulos { get; set; }
        }

        // ─── VISTA PRINCIPAL (INDEX) ──────────────────────────────────────────
        public ActionResult Index()
        {
            var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            var userId = Guid.Parse(userIdStr);

            using (var db = new ConexionModel())
            {
                var prov = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);
                if (prov == null) return Content("Proveedor no encontrado");

                var misVentas = db.detalle_pedidos
                    .Include("productos")
                    .Include("productos.categorias")
                    .Include("pedidos")
                    .Where(d => d.productos.proveedor_id == prov.id && d.pedidos.estado != "cancelado")
                    .ToList();

                var vm = new AnalisisViewModel();

                vm.TotalIngresos = misVentas.Sum(d => (decimal?)(d.cantidad * d.precio_unitario)) ?? 0;
                vm.TotalArticulosVendidos = misVentas.Sum(d => (int?)d.cantidad) ?? 0;

                vm.TopProductos = misVentas
                    .GroupBy(d => d.productos)
                    .Select(g => new ProductoVentas
                    {
                        Nombre = g.Key.nombre,
                        Sku = g.Key.sku,
                        CantidadVendida = g.Sum(x => x.cantidad),
                        IngresosGenerados = g.Sum(x => x.cantidad * x.precio_unitario)
                    })
                    .OrderByDescending(p => p.IngresosGenerados)
                    .Take(10)
                    .ToList();

                vm.VentasCategoria = misVentas
                    .GroupBy(d => d.productos.categorias)
                    .Select(g => new CategoriaVentas
                    {
                        Categoria = g.Key?.nombre ?? "Sin categoría",
                        Total = g.Sum(x => x.cantidad * x.precio_unitario)
                    })
                    .OrderByDescending(c => c.Total)
                    .ToList();

                // NUEVO: Cálculo de los últimos 6 meses
                vm.VentasPorMes = new List<VentaMensual>();
                for (int i = 5; i >= 0; i--)
                {
                    var fechaObj = DateTime.Now.AddMonths(-i);
                    var ventasMes = misVentas.Where(v => v.pedidos.creado_en.Year == fechaObj.Year && v.pedidos.creado_en.Month == fechaObj.Month).ToList();

                    vm.VentasPorMes.Add(new VentaMensual
                    {
                        Mes = fechaObj.ToString("MMM yyyy").ToUpper(),
                        Total = ventasMes.Sum(v => (decimal?)(v.cantidad * v.precio_unitario)) ?? 0,
                        Articulos = ventasMes.Sum(v => (int?)v.cantidad) ?? 0
                    });
                }

                return View(vm);
            }
        }

        // ─── DESCARGAR REPORTE EN PDF ─────────────────────────────────────────
        public ActionResult DescargarReportePdf()
        {
            var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return new HttpUnauthorizedResult();
            var userId = Guid.Parse(userIdStr);

            using (var db = new ConexionModel())
            {
                var prov = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);
                var misVentas = db.detalle_pedidos
                    .Include("productos")
                    .Include("pedidos")
                    .Where(d => d.productos.proveedor_id == prov.id && d.pedidos.estado != "cancelado")
                    .ToList();

                decimal totalIngresos = misVentas.Sum(d => (decimal?)(d.cantidad * d.precio_unitario)) ?? 0;
                int totalArticulos = misVentas.Sum(d => (int?)d.cantidad) ?? 0;

                var topProductos = misVentas
                    .GroupBy(d => d.productos)
                    .Select(g => new {
                        Nombre = g.Key.nombre,
                        Cantidad = g.Sum(x => x.cantidad),
                        Total = g.Sum(x => x.cantidad * x.precio_unitario)
                    })
                    .OrderByDescending(p => p.Total)
                    .Take(10)
                    .ToList();

                // NUEVO: Cálculo de meses para el PDF
                var ventasPorMes = new List<VentaMensual>();
                for (int i = 5; i >= 0; i--)
                {
                    var f = DateTime.Now.AddMonths(-i);
                    var v = misVentas.Where(x => x.pedidos.creado_en.Year == f.Year && x.pedidos.creado_en.Month == f.Month).ToList();
                    ventasPorMes.Add(new VentaMensual
                    {
                        Mes = f.ToString("MMM yyyy").ToUpper(),
                        Total = v.Sum(x => (decimal?)(x.cantidad * x.precio_unitario)) ?? 0,
                        Articulos = v.Sum(x => (int?)x.cantidad) ?? 0
                    });
                }

                string html = $@"
                <!DOCTYPE html>
                <html lang='es'>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: 'Helvetica', sans-serif; color: #333; margin: 40px; }}
                        .header {{ border-bottom: 3px solid #0d9488; padding-bottom: 10px; margin-bottom: 20px; }}
                        h1 {{ color: #0d9488; margin: 0; font-size: 24px; }}
                        .stats {{ display: table; width: 100%; margin-bottom: 30px; }}
                        .stat-box {{ display: table-cell; background: #f8fafc; padding: 15px; border-radius: 8px; width: 50%; border: 1px solid #e2e8f0; }}
                        table {{ width: 100%; border-collapse: collapse; margin-top: 15px; margin-bottom: 30px; }}
                        th {{ background-color: #0d9488; color: white; padding: 10px; text-align: left; font-size: 12px; }}
                        td {{ padding: 10px; border-bottom: 1px solid #e2e8f0; font-size: 13px; }}
                        h3 {{ color: #334155; border-bottom: 1px solid #cbd5e1; padding-bottom: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Reporte General de Ventas</h1>
                        <p style='margin: 5px 0 0 0; color: #64748b; font-size: 14px;'>Proveedor: <strong>{prov.nombre_empresa}</strong> | Fecha: {DateTime.Now.ToString("dd/MM/yyyy")}</p>
                    </div>

                    <div class='stats'>
                        <div class='stat-box' style='margin-right: 10px;'>
                            <p style='margin:0; font-size: 12px; color: #64748b;'>INGRESOS TOTALES HISTÓRICOS</p>
                            <p style='margin:5px 0 0 0; font-size: 24px; font-weight: bold; color: #0f172a;'>${totalIngresos.ToString("N2")}</p>
                        </div>
                        <div class='stat-box'>
                            <p style='margin:0; font-size: 12px; color: #64748b;'>ARTÍCULOS VENDIDOS</p>
                            <p style='margin:5px 0 0 0; font-size: 24px; font-weight: bold; color: #0f172a;'>{totalArticulos} unidades</p>
                        </div>
                    </div>

                    <!-- NUEVA TABLA DEL PDF: Reporte Mensual -->
                    <h3>Desglose Mensual (Últimos 6 meses)</h3>
                    <table>
                        <tr><th>Mes</th><th>Artículos Vendidos</th><th>Total Generado</th></tr>";

                foreach (var m in ventasPorMes)
                {
                    html += $"<tr><td>{m.Mes}</td><td>{m.Articulos}</td><td>${m.Total.ToString("N2")}</td></tr>";
                }

                html += @"
                    </table>

                    <h3>Top 10 Productos Más Vendidos</h3>
                    <table>
                        <tr><th>Producto</th><th>Cantidad</th><th>Ingresos</th></tr>";

                foreach (var p in topProductos)
                {
                    html += $"<tr><td>{p.Nombre}</td><td>{p.Cantidad}</td><td>${p.Total.ToString("N2")}</td></tr>";
                }

                html += @"
                    </table>
                </body>
                </html>";

                var renderer = new IronPdf.ChromePdfRenderer();
                var pdf = renderer.RenderHtmlAsPdf(html);

                return File(pdf.BinaryData, "application/pdf", $"Reporte_Ventas_{DateTime.Now.ToString("yyyyMMdd")}.pdf");
            }
        }
    }
}