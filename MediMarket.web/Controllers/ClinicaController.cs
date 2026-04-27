using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;

namespace MediMarket.web.Controllers
{
    public class ClinicaController : Controller
    {
        private Guid GetUserId()
        {
            return Guid.Parse(((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // GET: /Clinica/MiPerfil
        public ActionResult MiPerfil()
        {
            var userId = GetUserId();
            using (var db = new ConexionModel())
            {
                var clinica = db.clinicas.FirstOrDefault(c => c.usuario_id == userId);
                if (clinica == null) return HttpNotFound("No se encontró el perfil de la clínica.");

                return View(clinica);
            }
        }

        // POST: /Clinica/MiPerfil (Para guardar cambios)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MiPerfil(clinicas model)
        {
            ModelState.Remove("usuario_id"); // No viene del form

            if (!ModelState.IsValid) return View(model);

            var userId = GetUserId();
            using (var db = new ConexionModel())
            {
                var clinica = db.clinicas.FirstOrDefault(c => c.id == model.id && c.usuario_id == userId);
                if (clinica == null) return HttpNotFound();

                // Actualizamos los datos
                clinica.nombre_clinica = model.nombre_clinica;
                clinica.rfc = model.rfc;
                clinica.especialidad = model.especialidad;
                clinica.telefono = model.telefono;
                clinica.direccion = model.direccion;
                clinica.ciudad = model.ciudad;
                clinica.estado = model.estado;

                db.SaveChanges();
                TempData["Exito"] = "Datos de la clínica actualizados.";
                return RedirectToAction("MiPerfil");
            }
        }

        // POST: /Clinica/EliminarCuenta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarCuenta()
        {
            var userId = GetUserId();
            using (var db = new ConexionModel())
            {
                // Buscamos al USUARIO. Al borrarlo, se borra la clínica por el CASCADE en SQL
                var usuario = db.usuarios.FirstOrDefault(u => u.id == userId);
                if (usuario != null)
                {
                    db.usuarios.Remove(usuario);
                    db.SaveChanges();
                }
            }

            // Lo mandamos a tu método de Logout para que cierre sesión y limpie cookies
            return RedirectToAction("Logout", "Account");
        }
    }
}