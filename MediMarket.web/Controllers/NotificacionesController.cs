using System;
using System.Linq;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;

namespace MediMarket.web.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        [HttpPost]
        public JsonResult MarcarLeida(Guid id)
        {
            using (var db = new ConexionModel())
            {
                var noti = db.notificaciones_clinicas.Find(id);
                if (noti != null)
                {
                    noti.leida = true;
                    db.SaveChanges();
                    return Json(new { ok = true });
                }
                return Json(new { ok = false });
            }
        }

        [HttpPost]
        public JsonResult MarcarTodas()
        {
            using (var db = new ConexionModel())
            {
                var uidStr = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(uidStr)) return Json(new { ok = false });

                var uid = Guid.Parse(uidStr);
                var clinica = db.clinicas.FirstOrDefault(c => c.usuario_id == uid);

                if (clinica != null)
                {
                    var pendientes = db.notificaciones_clinicas
                                       .Where(n => n.clinica_id == clinica.id && !n.leida)
                                       .ToList();

                    pendientes.ForEach(n => n.leida = true);
                    db.SaveChanges();
                    return Json(new { ok = true });
                }
                return Json(new { ok = false });
            }
        }
    }
}