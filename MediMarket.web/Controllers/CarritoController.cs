using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MediMarket.web.Models;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using System.Configuration;

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

                    var productoDB = db.productos.Find(item.ProductoId);
                    if (productoDB != null)
                    {
                        productoDB.stock_disponible -= item.Cantidad;
                        proveedoresInvolucrados.Add(productoDB.proveedor_id);
                    }
                }

                // 5. Crear Notificaciones para los proveedores involucrados
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

                var notiClinica = new notificaciones_clinicas
                {
                    id = Guid.NewGuid(),
                    clinica_id = miClinica.id,
                    titulo = "¡Pedido Confirmado!",
                    mensaje = $"Tu orden #{nuevoPedido.numero_pedido} se ha generado con éxito por un total de ${totalPago.ToString("N2")}.",
                    tipo = "orden",
                    leida = false,
                    creado_en = DateTime.Now
                };
                db.notificaciones_clinicas.Add(notiClinica);

                // 6. Guardar en SQL
                db.SaveChanges();

                // 7. Vaciar carrito
                Session.Remove("Carrito");

                // 8. Enviar correo en segundo plano
                var correoUsuario = ((System.Security.Claims.ClaimsIdentity)User.Identity).FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "correo@destino.com";
                var articulosComprados = carrito.ToList(); 
                var idPedido = nuevoPedido.id;

                Task.Run(() => EnviarCorreoConfirmacion(correoUsuario, numOrden, totalPago.ToString("N2"), miClinica.nombre_clinica, articulosComprados, idPedido));

                // 9. Pasar ID a la vista de éxito
                TempData["PedidoId"] = nuevoPedido.id;

                return RedirectToAction("Exito");
            }
        }

        // ─── LA NUEVA VISTA DE ÉXITO (Con datos de la clínica) ───
        public ActionResult Exito()
        {
            if (TempData["PedidoId"] == null) return RedirectToAction("Index", "Shop");

            Guid pedidoId = (Guid)TempData["PedidoId"];
            using (var db = new ConexionModel())
            {
                var pedido = db.pedidos.Include("clinicas").FirstOrDefault(p => p.id == pedidoId);
                return View(pedido);
            }
        }

        // ─── MÉTODO PARA MANDAR EL CORREO 
        private void EnviarCorreoConfirmacion(string destinatario, string numOrden, string total, string nombreClinica, List<CarritoItem> articulos, Guid pedidoId)
        {
            try
            {
                string miCorreo = ConfigurationManager.AppSettings["EmailSoporte"];
                string miPassword = ConfigurationManager.AppSettings["EmailPassword"];

                if (string.IsNullOrEmpty(miCorreo) || string.IsNullOrEmpty(miPassword)) return; 

                // 1. Armamos las filas de los productos con un ciclo
                string filasProductos = "";
                foreach(var item in articulos)
                {
                    filasProductos += $@"
                    <tr>
                        <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6;'>
                            <strong style='color: #111827; font-size: 14px;'>{item.Nombre}</strong><br>
                            <span style='color: #6b7280; font-size: 11px;'>Vendido por: {item.Proveedor}</span>
                        </td>
                        <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; text-align: center; color: #4b5563; font-size: 14px;'>
                            {item.Cantidad}
                        </td>
                        <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; text-align: right; color: #111827; font-size: 14px; font-weight: bold;'>
                            ${item.Subtotal.ToString("N2")}
                        </td>
                    </tr>";
                }

                // OJO: Cambia este puerto (44341) si tu Visual Studio usa uno diferente
                string urlBoton = $"https://localhost:44341/Shop/MisPedidos"; 

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(miCorreo, "MediMarket");
                mail.To.Add(destinatario);
                mail.Subject = $"Confirmación de Orden #{numOrden} - MediMarket";
                mail.IsBodyHtml = true;

                // 2. Maquetación del correo con tabla y botón
                mail.Body = $@"
                <div style='font-family: Arial, sans-serif; background-color: #f9fafb; padding: 40px 20px; color: #374151;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border: 1px solid #e5e7eb;'>
                        
                        <!-- Header Verde -->
                        <div style='background-color: #0f766e; padding: 40px; color: white;'>
                            <p style='margin: 0; font-size: 11px; font-weight: bold; text-transform: uppercase; letter-spacing: 1px; color: #99f6e4;'>Recibo de compra</p>
                            <h1 style='margin: 10px 0; font-size: 24px;'>Orden #{numOrden}</h1>
                            <p style='margin: 0; font-size: 14px; color: #ccfbf1;'>Tu pedido ha sido procesado con éxito.</p>
                        </div>

                        <!-- Cuerpo del recibo -->
                        <div style='padding: 40px;'>
                            <h3 style='margin-top: 0; color: #111827; font-size: 16px; text-transform: uppercase; font-size: 12px; letter-spacing: 1px;'>Enviar a:</h3>
                            <p style='margin: 5px 0 25px 0; font-size: 16px;'><strong>{nombreClinica}</strong></p>
                            
                            <!-- Tabla de productos -->
                            <table style='width: 100%; border-collapse: collapse; margin-bottom: 25px;'>
                                <thead>
                                    <tr>
                                        <th style='text-align: left; padding-bottom: 10px; border-bottom: 2px solid #e5e7eb; color: #9ca3af; font-size: 10px; text-transform: uppercase;'>Producto</th>
                                        <th style='text-align: center; padding-bottom: 10px; border-bottom: 2px solid #e5e7eb; color: #9ca3af; font-size: 10px; text-transform: uppercase;'>Cant.</th>
                                        <th style='text-align: right; padding-bottom: 10px; border-bottom: 2px solid #e5e7eb; color: #9ca3af; font-size: 10px; text-transform: uppercase;'>Subtotal</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filasProductos}
                                </tbody>
                            </table>

                            <!-- Total -->
                            <div style='background-color: #f3f4f6; padding: 20px; border-radius: 8px; margin-bottom: 35px;'>
                                <p style='margin: 0; display: flex; justify-content: space-between; align-items: center;'>
                                    <span style='color: #6b7280; font-weight: bold;'>TOTAL PAGADO:</span> 
                                    <strong style='color: #0f766e;'>${total} MXN</strong>
                                </p>
                            </div>

                            <!-- Botón de acción -->
                            <div style='text-align: center;'>
                                <a href='{urlBoton}' style='display: inline-block; background-color: #0f766e; color: #ffffff; text-decoration: none; font-weight: bold; padding: 14px 28px; border-radius: 8px; font-size: 14px;'>
                                    Ver estatus del pedido
                                </a>
                            </div>
                            
                            <p style='font-size: 11px; color: #9ca3af; margin-top: 40px; text-align: center;'>
                                Este documento sirve como comprobante de tu solicitud en la plataforma MediMarket.
                            </p>
                        </div>
                    </div>
                </div>";

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(miCorreo, miPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al enviar correo: " + ex.Message);
            }
        }
    }
}