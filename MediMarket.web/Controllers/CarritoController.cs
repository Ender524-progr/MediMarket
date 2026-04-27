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

                    // Obtener o crear el carrito en la sesión
                    var carrito = Session["Carrito"] as List<CarritoItem> ?? new List<CarritoItem>();

                    // ¿Ya está el producto en el carrito?
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
    }
}