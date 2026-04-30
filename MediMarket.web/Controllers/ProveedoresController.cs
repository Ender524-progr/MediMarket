using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;

namespace MediMarket.web.Controllers
{
    public class ProveedoresController : BaseProveedorController
    {
        // Modelo para organizar los datos del dashboard
        public class DashboardViewModel
        {
            public decimal IngresosTotales { get; set; }
            public int PedidosPendientes { get; set; }
            public int ProductosActivos { get; set; }
            public double CalificacionPromedio { get; set; }
            public List<pedidos> UltimosPedidos { get; set; }
            public List<productos> ProductosPocoStock { get; set; }

            // Datos para la gráfica
            public List<string> Meses { get; set; }
            public List<decimal> VentasPorMes { get; set; }
        }

        public ActionResult Index()
        {
            var identity = (ClaimsIdentity)User.Identity;
            ViewBag.Nombre = identity.FindFirst(ClaimTypes.Name)?.Value ?? "Proveedor";
            ViewBag.FotoUrl = identity.FindFirst("foto_url")?.Value;

            var userIdStr = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            var userId = Guid.Parse(userIdStr);

            using (var db = new ConexionModel())
            {
                // Obtenemos al proveedor actual
                var prov = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);
                if (prov == null) return Content("Proveedor no encontrado");

                var vm = new DashboardViewModel();

                // 1. Productos Activos
                vm.ProductosActivos = db.productos.Count(p => p.proveedor_id == prov.id && p.activo);

                // 2. Pedidos y Detalles (donde hay productos de este proveedor)
                var misPedidos = db.pedidos
                    .Include("detalle_pedidos")
                    .Include("detalle_pedidos.productos")
                    .Include("clinicas")
                    .Where(p => p.detalle_pedidos.Any(d => d.productos.proveedor_id == prov.id))
                    .ToList();

                var misDetalles = db.detalle_pedidos
                    .Include("pedidos")
                    .Where(d => d.productos.proveedor_id == prov.id && d.pedidos.estado != "cancelado")
                    .ToList();

                // 3. Cálculos Rápidos (Tarjetas)
                vm.PedidosPendientes = misPedidos.Count(p => p.estado == "confirmado" || p.estado == "en_proceso");
                vm.IngresosTotales = misDetalles.Any() ? misDetalles.Sum(d => d.cantidad * d.precio_unitario) : 0;

                var misComentarios = db.producto_comentarios.Where(c => c.productos.proveedor_id == prov.id).ToList();
                vm.CalificacionPromedio = misComentarios.Any() ? misComentarios.Average(c => c.calificacion) : 0;

                // 4. Tablas de la vista
                vm.UltimosPedidos = misPedidos.OrderByDescending(p => p.creado_en).Take(5).ToList();
                vm.ProductosPocoStock = db.productos
                    .Where(p => p.proveedor_id == prov.id && p.stock_disponible <= 10 && p.activo)
                    .OrderBy(p => p.stock_disponible)
                    .Take(5)
                    .ToList();

                // 5. Datos para la Gráfica (Últimos 6 meses)
                vm.Meses = new List<string>();
                vm.VentasPorMes = new List<decimal>();
                for (int i = 5; i >= 0; i--)
                {
                    var mesObjetivo = DateTime.Now.AddMonths(-i);
                    vm.Meses.Add(mesObjetivo.ToString("MMM yyyy")); // Ej. "Abr 2026"

                    var ventasMes = misDetalles
                        .Where(d => d.pedidos.creado_en.Year == mesObjetivo.Year && d.pedidos.creado_en.Month == mesObjetivo.Month)
                        .Sum(d => d.cantidad * d.precio_unitario);

                    vm.VentasPorMes.Add(ventasMes);
                }

                return View(vm);
            }
        }

        public ActionResult RFQs()
        {
            return RedirectToAction("Index", "SolicitudesRfq");
        }

        // ─── VER PERFIL DE EMPRESA ───────────────────────────────────────────
        public ActionResult MiEmpresa()
        {
            var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            var userId = Guid.Parse(userIdStr);

            using (var db = new ConexionModel())
            {
                var prov = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);
                if (prov == null) return HttpNotFound("Proveedor no encontrado");

                return View(prov);
            }
        }

        // ─── GUARDAR PERFIL Y SUBIR FOTOS ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MiEmpresa(proveedores modeloActualizado, System.Web.HttpPostedFileBase logoFile, System.Web.HttpPostedFileBase bannerFile)
        {
            var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdStr);

            using (var db = new ConexionModel())
            {
                var provDB = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);
                if (provDB == null) return HttpNotFound();

                // 1. Actualizamos datos de texto
                provDB.nombre_empresa = modeloActualizado.nombre_empresa;
                provDB.rfc = modeloActualizado.rfc;
                provDB.categoria_principal = modeloActualizado.categoria_principal;
                provDB.telefono = modeloActualizado.telefono;
                provDB.cp = modeloActualizado.cp;
                provDB.direccion = modeloActualizado.direccion;
                provDB.registro_sanitario = modeloActualizado.registro_sanitario;
                provDB.eslogan = modeloActualizado.eslogan;

                // 2. Lógica para guardar la foto del LOGO
                if (logoFile != null && logoFile.ContentLength > 0)
                {
                    string ext = System.IO.Path.GetExtension(logoFile.FileName);
                    string fileName = "logo_" + provDB.id + ext;
                    string path = System.IO.Path.Combine(Server.MapPath("~/Content/Uploads/Empresas/"), fileName);
                    logoFile.SaveAs(path);
                    provDB.logo_url = "/Content/Uploads/Empresas/" + fileName;
                }

                // 3. Lógica para guardar el BANNER
                if (bannerFile != null && bannerFile.ContentLength > 0)
                {
                    string ext = System.IO.Path.GetExtension(bannerFile.FileName);
                    string fileName = "banner_" + provDB.id + ext;
                    string path = System.IO.Path.Combine(Server.MapPath("~/Content/Uploads/Empresas/"), fileName);
                    bannerFile.SaveAs(path);
                    provDB.banner_url = "/Content/Uploads/Empresas/" + fileName;
                }

                db.SaveChanges();
                TempData["Exito"] = "Perfil de empresa actualizado correctamente.";

                return RedirectToAction("MiEmpresa");
            }
        }

        // ─── VER CONFIGURACIÓN ───────────────────────────────────────────────
        public ActionResult Configuracion()
        {
            var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            var userId = Guid.Parse(userIdStr);

            using (var db = new ConexionModel())
            {
                var prov = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);
                if (prov == null) return HttpNotFound("Proveedor no encontrado");

                return View(prov);
            }
        }

        // ─── GUARDAR CONFIGURACIÓN ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Configuracion(proveedores modeloActualizado)
        {
            var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdStr);

            using (var db = new ConexionModel())
            {
                var provDB = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);
                if (provDB == null) return HttpNotFound();

                // Actualizamos Notificaciones
                provDB.notificar_rfq = modeloActualizado.notificar_rfq;
                provDB.notificar_ordenes = modeloActualizado.notificar_ordenes;
                provDB.notificar_inventario = modeloActualizado.notificar_inventario;

                // Actualizamos Bancos
                provDB.cuenta_clabe = modeloActualizado.cuenta_clabe;
                provDB.banco = modeloActualizado.banco;
                provDB.regimen_fiscal = modeloActualizado.regimen_fiscal;

                // Actualizamos Logística
                provDB.dias_preparacion = modeloActualizado.dias_preparacion;
                provDB.monto_envio_gratis = modeloActualizado.monto_envio_gratis;

                db.SaveChanges();
                TempData["ExitoConfig"] = "Tu configuración se ha guardado correctamente.";

                return RedirectToAction("Configuracion");
            }
        }
    }
}