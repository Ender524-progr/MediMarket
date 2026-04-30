using System;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using MediMarket.web.Models;

namespace MediMarket.web.Controllers
{
    public class BaseProveedorController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Solo intentamos buscar si el usuario está autenticado
            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdStr, out Guid userId))
                {
                    using (var db = new ConexionModel())
                    {
                        // Buscamos la empresa ligada a este usuario
                        var proveedor = db.proveedores.FirstOrDefault(p => p.usuario_id == userId);

                        if (proveedor != null)
                        {
                            // Datos de la empresa
                            ViewBag.NombreEmpresa = proveedor.nombre_empresa;
                            ViewBag.RfcEmpresa = proveedor.rfc;

                            // Buscar las notificaciones en la nueva tabla usando el id del proveedor
                            var misNotificaciones = db.notificaciones_proveedores
                                .Where(n => n.proveedor_id == proveedor.id && !n.leida)
                                .OrderByDescending(n => n.creado_en)
                                .Take(5)
                                .ToList();

                            ViewBag.Notificaciones = misNotificaciones;
                            ViewBag.TotalNotificaciones = misNotificaciones.Count;
                        }
                    }
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}