using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MediMarket.web.Models;

namespace MediMarket.web.Controllers
{
    [Authorize]
    public class CarritoController : Controller
    {
        // Modelo temporal para el carrito en memoria
        public class CarritoItem
        {
            public Guid ProductoId { get; set; }
            public string Nombre { get; set; }
            public string Imagen { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
            public string Proveedor { get; set; }
            public decimal Subtotal => Precio * Cantidad;
        }

        // GET: /Carrito/
        public ActionResult Index()
        {
            var carrito = Session["Carrito"] as List<CarritoItem> ?? new List<CarritoItem>();
            return View(carrito);
        }

        // POST: /Carrito/Agregar
        [HttpPost]
        public JsonResult Agregar(Guid productoId, int cantidad = 1)
        {
            try
            {
                using (var db = new ConexionModel())
                {
                    var producto = db.productos
                        .Include("producto_imagenes")
                        .Include("proveedores")
                        .FirstOrDefault(p => p.id == productoId);

                    if (producto == null) return Json(new { ok = false, mensaje = "Producto no encontrado" });

                    var carrito = Session["Carrito"] as List<CarritoItem> ?? new List<CarritoItem>();

                    var itemExistente = carrito.FirstOrDefault(i => i.ProductoId == productoId);

                    if (itemExistente != null)
                    {
                        itemExistente.Cantidad += cantidad;
                    }
                    else
                    {
                        carrito.Add(new CarritoItem
                        {
                            ProductoId = producto.id,
                            Nombre = producto.nombre,
                            Precio = producto.precio_unitario,
                            Cantidad = cantidad,
                            Proveedor = producto.proveedores?.nombre_empresa,
                            Imagen = producto.producto_imagenes?.FirstOrDefault(i => i.es_principal)?.url ?? "/Content/img/no-image.png"
                        });
                    }

                    Session["Carrito"] = carrito;
                    return Json(new { ok = true, contador = carrito.Sum(i => i.Cantidad) });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = "Error: " + ex.Message });
            }
        }

        [HttpPost]
public JsonResult ActualizarCantidad(Guid productoId, int cantidad)
{
    if (cantidad < 1) return Json(new { ok = false, mensaje = "La cantidad mínima es 1" });

    try
    {
        var carrito = Session["Carrito"] as List<CarritoItem>;
        if (carrito == null) return Json(new { ok = false, mensaje = "El carrito está vacío" });

        var item = carrito.FirstOrDefault(i => i.ProductoId == productoId);
        if (item == null) return Json(new { ok = false, mensaje = "Producto no encontrado en el carrito" });

        using (var db = new ConexionModel())
        {
            // Verificamos el stock real en la BD
            var producto = db.productos.FirstOrDefault(p => p.id == productoId);
            if (producto == null) return Json(new { ok = false, mensaje = "Producto no encontrado" });

            if (cantidad > producto.stock_disponible)
            {
                return Json(new { ok = false, mensaje = $"¡Ups! Solo hay {producto.stock_disponible} unidades disponibles" });
            }

            // 1. Actualizamos la memoria
            item.Cantidad = cantidad;
            Session["Carrito"] = carrito;

            // 2. Recalculamos toda la matemática exactamente como en tu vista
            int totalArticulos = carrito.Sum(i => i.Cantidad);
            decimal subtotal = carrito.Sum(i => i.Subtotal);
            decimal envio = 50 * totalArticulos; 
            decimal totalPago = subtotal + envio;

            // 3. Devolvemos los números ya formateados al frontend
            return Json(new {
                ok = true,
                nuevoSubtotalLinea = item.Subtotal.ToString("N2"),
                nuevoSubtotalCarrito = subtotal.ToString("N2"),
                nuevoEnvio = envio.ToString("N2"),
                nuevoTotal = totalPago.ToString("N2"),
                totalArticulos = totalArticulos
            });
        }
    }
    catch (Exception ex)
    {
        return Json(new { ok = false, mensaje = "Error: " + ex.Message });
    }
}

        // POST: /Carrito/Eliminar
        [HttpPost]
        public ActionResult Eliminar(Guid productoId)
        {
            var carrito = Session["Carrito"] as List<CarritoItem>;
            if (carrito != null)
            {
                carrito.RemoveAll(i => i.ProductoId == productoId);
                Session["Carrito"] = carrito;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ProcesarPagoFalso()
        {
            var carrito = Session["Carrito"] as List<CarritoItem>;
            if (carrito == null || !carrito.Any()) return RedirectToAction("Index", "Shop");

            using (var db = new ConexionModel())
            {
                // 1. Identificar a la clínica
                var userIdStr = ((System.Security.Claims.ClaimsIdentity)User.Identity).FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var uId = Guid.Parse(userIdStr);
                var miClinica = db.clinicas.FirstOrDefault(c => c.usuario_id == uId);

                if (miClinica == null) return RedirectToAction("Index", "Shop");

                // 2. Matemáticas finales
                int totalArticulos = carrito.Sum(i => i.Cantidad);
                decimal subtotal = carrito.Sum(i => i.Subtotal);
                decimal envio = 50 * totalArticulos;
                decimal totalPago = subtotal + envio;

                string numOrden = $"ORD-{new Random().Next(1000, 9999)}-MC";

                // 3. Crear el Pedido Principal
                var nuevoPedido = new pedidos
                {
                    id = Guid.NewGuid(),
                    clinica_id = miClinica.id,
                    numero_pedido = numOrden,
                    estado = "confirmado",
                    total = totalPago,
                    metodo_pago = "Simulación Efectivo",
                    creado_en = DateTime.Now
                };
                db.pedidos.Add(nuevoPedido);

                // ---> NUEVO: Creamos una lista "inteligente" para guardar IDs sin repetirlos
                var proveedoresInvolucrados = new HashSet<Guid>();

                // 4. Crear los Detalles del Pedido y Restar Stock
                foreach (var item in carrito)
                {
                    var detalle = new detalle_pedidos
                    {
                        id = Guid.NewGuid(),
                        pedido_id = nuevoPedido.id,
                        producto_id = item.ProductoId,
                        cantidad = item.Cantidad,
                        precio_unitario = item.Precio
                    };
                    db.detalle_pedidos.Add(detalle);

                    // Buscamos el producto en la BD para restar stock y sacar el ID de su proveedor
                    var productoDB = db.productos.Find(item.ProductoId);
                    if (productoDB != null)
                    {
                        productoDB.stock_disponible -= item.Cantidad;

                        // Guardamos a este proveedor en nuestra lista
                        proveedoresInvolucrados.Add(productoDB.proveedor_id);
                    }
                }

                // ---> NUEVO: 5. Crear Notificaciones para los proveedores involucrados
                foreach (var provId in proveedoresInvolucrados)
                {
                    var nuevaNotificacion = new notificaciones_proveedores
                    {
                        id = Guid.NewGuid(),
                        proveedor_id = provId,
                        titulo = "¡Nueva Orden Recibida!",
                        mensaje = $"La clínica acaba de generar un pedido (#{nuevoPedido.numero_pedido}). Revisa tu panel para prepararlo.",
                        tipo = "orden",
                        leida = false,
                        creado_en = DateTime.Now
                    };

                    db.notificaciones_proveedores.Add(nuevaNotificacion);
                }

                // 6. Guardar en SQL todo de un solo golpe (pedido, detalles, stock y notificaciones)
                db.SaveChanges();

                // Vaciar carrito
                Session.Remove("Carrito");

                // Mandar datos a la pantalla final
                TempData["NumeroOrden"] = numOrden;
                TempData["TotalPago"] = totalPago;

                return RedirectToAction("Exito");
            }
        }

        // La vista que carga tu diseño de éxito
        public ActionResult Exito()
{
    // Si entran de chismosos sin haber comprado, los pateamos a la tienda
    if (TempData["NumeroOrden"] == null) return RedirectToAction("Index", "Shop");
    
    return View();
}
    }
}