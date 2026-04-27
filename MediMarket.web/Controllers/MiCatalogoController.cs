using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;
using MediMarket.web.Models.ViewModels;
using System.IO;

namespace MediMarket.web.Controllers
{
    public class MiCatalogoController : BaseProveedorController
    {
        // ─── Helpers ────────────────────────────────────────────────────────────

        // Obtiene el registro de proveedor del usuario logueado
        private proveedores GetProveedorActual(ConexionModel db)
        {
            var usuarioId = Guid.Parse(
                ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value
            );
            return db.proveedores.FirstOrDefault(p => p.usuario_id == usuarioId);
        }

        // Carga las categorías para el dropdown
        private IEnumerable<categorias> GetCategorias(ConexionModel db)
        {
            return db.categorias.OrderBy(c => c.nombre).ToList();
        }

        // Guarda imágenes en ~/Content/uploads/productos/ y retorna las URLs
        private List<string> GuardarImagenes(IEnumerable<HttpPostedFileBase> archivos)
        {
            var urls = new List<string>();
            if (archivos == null) return urls;

            var carpeta = Server.MapPath("~/Content/uploads/productos/");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

            foreach (var archivo in archivos)
            {
                if (archivo == null || archivo.ContentLength == 0) continue;

                var extension = Path.GetExtension(archivo.FileName).ToLower();
                var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (!permitidas.Contains(extension)) continue;

                var nombreArchivo = Guid.NewGuid() + extension;
                archivo.SaveAs(Path.Combine(carpeta, nombreArchivo));
                urls.Add("/Content/uploads/productos/" + nombreArchivo);
            }

            return urls;
        }

        // ─── INDEX ───────────────────────────────────────────────────────────────

        public ActionResult Index()
        {
            using (var db = new ConexionModel())
            {
                var proveedor = GetProveedorActual(db);
                if (proveedor == null) return RedirectToAction("Login", "Account");

                // En MiCatalogoController.cs - acción Index
                var productos = db.productos
                    .Include("producto_imagenes")  // ← agregar esto
                    .Where(p => p.proveedor_id == proveedor.id)
                    .OrderByDescending(p => p.creado_en)
                    .ToList();
                //Categorias
                var categorias = db.categorias
                    .OrderBy(c => c.nombre)
                    .ToList();
                ViewBag.Categorias = categorias;

                return View(productos);
            }
        }

        // ─── CREATE ──────────────────────────────────────────────────────────────

        public ActionResult Create()
        {
            using (var db = new ConexionModel())
            {
                var vm = new ProductoViewModel
                {
                    Categorias = GetCategorias(db),
                    Activo = true
                };
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductoViewModel vm, IEnumerable<HttpPostedFileBase> Imagenes)
        {
            using (var db = new ConexionModel())
            {
                if (!ModelState.IsValid)
                {
                    vm.Categorias = GetCategorias(db);
                    return View(vm);
                }

                var proveedor = GetProveedorActual(db);
                if (proveedor == null) return RedirectToAction("Login", "Account");

                var producto = new productos
                {
                    id = Guid.NewGuid(),
                    proveedor_id = proveedor.id,
                    categoria_id = vm.CategoriaId,
                    nombre = vm.Nombre,
                    descripcion = vm.Descripcion,
                    sku = vm.Sku,
                    precio_unitario = vm.PrecioUnitario,
                    unidad_medida = vm.UnidadMedida,
                    stock_disponible = vm.StockDisponible,
                    activo = vm.Activo,
                    creado_en = DateTime.Now
                };
                db.productos.Add(producto);

                // Guardar imágenes
                var urls = GuardarImagenes(Imagenes);
                for (int i = 0; i < urls.Count; i++)
                {
                    db.producto_imagenes.Add(new producto_imagenes
                    {
                        id = Guid.NewGuid(),
                        producto_id = producto.id,
                        url = urls[i],
                        es_principal = i == 0, // la primera es la principal
                        orden = i
                    });
                }

                db.SaveChanges();
                TempData["Exito"] = "Producto creado correctamente.";
                return RedirectToAction("Index");
            }
        }

        // ─── DETAILS ─────────────────────────────────────────────────────────────

        public ActionResult Details(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var proveedor = GetProveedorActual(db);
                var producto = db.productos
                    .Include("categorias")
                    .Include("producto_imagenes")
                    .FirstOrDefault(p => p.id == id && p.proveedor_id == proveedor.id);

                if (producto == null) return HttpNotFound();
                return View(producto);
            }
        }

        // ─── EDIT ────────────────────────────────────────────────────────────────

        public ActionResult Edit(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var proveedor = GetProveedorActual(db);
                var producto = db.productos
                    .Include("producto_imagenes")
                    .FirstOrDefault(p => p.id == id && p.proveedor_id == proveedor.id);

                if (producto == null) return HttpNotFound();

                var vm = new ProductoViewModel
                {
                    Id = producto.id,
                    Nombre = producto.nombre,
                    Descripcion = producto.descripcion,
                    Sku = producto.sku,
                    PrecioUnitario = producto.precio_unitario,
                    UnidadMedida = producto.unidad_medida,
                    StockDisponible = producto.stock_disponible,
                    Activo = producto.activo,
                    CategoriaId = producto.categoria_id,
                    Categorias = GetCategorias(db),
                    ImagenesActuales = producto.producto_imagenes.OrderBy(i => i.orden).ToList()
                };
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductoViewModel vm, IEnumerable<HttpPostedFileBase> Imagenes, IEnumerable<Guid> ImagenesEliminar)
        {
            using (var db = new ConexionModel())
            {
                if (!ModelState.IsValid)
                {
                    vm.Categorias = GetCategorias(db);
                    vm.ImagenesActuales = db.producto_imagenes
                        .Where(i => i.producto_id == vm.Id)
                        .OrderBy(i => i.orden).ToList();
                    return View(vm);
                }

                var proveedor = GetProveedorActual(db);
                var producto = db.productos
                    .FirstOrDefault(p => p.id == vm.Id && p.proveedor_id == proveedor.id);

                if (producto == null) return HttpNotFound();

                // Actualizar campos
                producto.nombre = vm.Nombre;
                producto.descripcion = vm.Descripcion;
                producto.sku = vm.Sku;
                producto.precio_unitario = vm.PrecioUnitario;
                producto.unidad_medida = vm.UnidadMedida;
                producto.stock_disponible = vm.StockDisponible;
                producto.activo = vm.Activo;
                producto.categoria_id = vm.CategoriaId;

                // Eliminar imágenes marcadas
                if (ImagenesEliminar != null)
                {
                    foreach (var imgId in ImagenesEliminar)
                    {
                        var img = db.producto_imagenes.Find(imgId);
                        if (img != null)
                        {
                            var rutaFisica = Server.MapPath(img.url);
                            if (System.IO.File.Exists(rutaFisica))
                                System.IO.File.Delete(rutaFisica);
                            db.producto_imagenes.Remove(img);
                        }
                    }
                }

                // Agregar nuevas imágenes
                var urls = GuardarImagenes(Imagenes);
                var ordenActual = db.producto_imagenes
                    .Where(i => i.producto_id == producto.id)
                    .Count();

                foreach (var url in urls)
                {
                    db.producto_imagenes.Add(new producto_imagenes
                    {
                        id = Guid.NewGuid(),
                        producto_id = producto.id,
                        url = url,
                        es_principal = ordenActual == 0,
                        orden = ordenActual++
                    });
                }

                db.SaveChanges();
                TempData["Exito"] = "Producto actualizado correctamente.";
                return RedirectToAction("Index");
            }
        }

        // ─── DELETE ──────────────────────────────────────────────────────────────

        public ActionResult Delete(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var proveedor = GetProveedorActual(db);
                var producto = db.productos
                    .Include("producto_imagenes")
                    .FirstOrDefault(p => p.id == id && p.proveedor_id == proveedor.id);

                if (producto == null) return HttpNotFound();
                return View(producto);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var proveedor = GetProveedorActual(db);
                var producto = db.productos
                    .Include("producto_imagenes")
                    .FirstOrDefault(p => p.id == id && p.proveedor_id == proveedor.id);

                if (producto == null) return HttpNotFound();

                // Eliminar archivos físicos
                foreach (var img in producto.producto_imagenes.ToList())
                {
                    var rutaFisica = Server.MapPath(img.url);
                    if (System.IO.File.Exists(rutaFisica))
                        System.IO.File.Delete(rutaFisica);
                }

                db.productos.Remove(producto);
                db.SaveChanges();

                TempData["Exito"] = "Producto eliminado correctamente.";
                return RedirectToAction("Index");
            }
        }

        // ─── TOGGLE ACTIVO (acción rápida desde el Index) ────────────────────────

        [HttpPost]
        public ActionResult ToggleActivo(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var proveedor = GetProveedorActual(db);
                var producto = db.productos
                    .FirstOrDefault(p => p.id == id && p.proveedor_id == proveedor.id);

                if (producto == null) return HttpNotFound();

                producto.activo = !producto.activo;
                db.SaveChanges();

                return Json(new { activo = producto.activo });
            }
        }
    }
}