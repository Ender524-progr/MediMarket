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
        public ActionResult MiPerfil(Guid? id)
        {
            var userId = GetUserId();
            using (var db = new ConexionModel())
            {
                // 1. Traemos TODAS las clínicas para la columna izquierda
                var misClinicas = db.clinicas.Where(c => c.usuario_id == userId).ToList();
                ViewBag.MisClinicas = misClinicas;

                if (!misClinicas.Any()) return RedirectToAction("Nueva"); // Por si no tiene ninguna

                // 2. Determinamos cuál vamos a editar (la que pidió por URL, o la de la Sesión, o la primera)
                var activaId = id ?? Session["ClinicaActivaId"] as Guid?;
                var clinicaAEditar = misClinicas.FirstOrDefault(c => c.id == activaId) ?? misClinicas.FirstOrDefault();

                return View(clinicaAEditar);
            }
        }

        // POST: /Clinica/MiPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MiPerfil(clinicas model)
        {
            ModelState.Remove("usuario_id"); 
            var userId = GetUserId();

            using (var db = new ConexionModel())
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.MisClinicas = db.clinicas.Where(c => c.usuario_id == userId).ToList();
                    return View(model);
                }

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
                
                // Recargamos la vista con la misma clínica
                return RedirectToAction("MiPerfil", new { id = model.id });
            }
        }

        public ActionResult Nueva()
        {
            var userId = GetUserId();
            using (var db = new ConexionModel())
            {
                ViewBag.MisClinicas = db.clinicas.Where(c => c.usuario_id == userId).ToList();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Nueva(string nombreClinica, string rfc, string cedula, string especialidad, string telefono, string cp, string estado, string municipio, string colonia, string calle, string num_ext, string num_int)
        {
            var userId = GetUserId();
            using (var db = new ConexionModel())
            {
                // Armamos la dirección completa
                string direccionCompleta = $"{calle} {num_ext}";
                if (!string.IsNullOrWhiteSpace(num_int)) direccionCompleta += $" Int. {num_int}";
                direccionCompleta += $", Col. {colonia}, C.P. {cp}";

                var nuevaClinica = new clinicas
                {
                    id = Guid.NewGuid(),
                    usuario_id = userId,
                    nombre_clinica = nombreClinica,
                    rfc = rfc,
                    cedula_profesional = cedula,
                    especialidad = especialidad,
                    telefono = telefono,
                    cp = cp,
                    estado = estado,
                    ciudad = municipio,
                    direccion = direccionCompleta
                };

                db.clinicas.Add(nuevaClinica);
                db.SaveChanges();

                // Automáticamente la ponemos como activa en la sesión
                Session["ClinicaActivaId"] = nuevaClinica.id;
                TempData["Exito"] = "¡Nueva clínica registrada!";
                return RedirectToAction("MiPerfil", new { id = nuevaClinica.id });
            }
        }

        [Authorize]
public ActionResult SwitchClinica(Guid id)
{
    using (var db = new ConexionModel())
    {
        // Validamos que la clínica que quiere seleccionar SÍ sea de él
        var userIdStr = ((System.Security.Claims.ClaimsIdentity)User.Identity).FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var uId = Guid.Parse(userIdStr);
        
        var clinicaDestino = db.clinicas.FirstOrDefault(c => c.id == id && c.usuario_id == uId);
        
        if (clinicaDestino != null)
        {
            // Guardamos el nuevo ID en la sesión
            Session["ClinicaActivaId"] = clinicaDestino.id;
        }
        
        // Lo regresamos a donde estaba (o al inicio si no sabemos)
        if (Request.UrlReferrer != null)
        {
            return Redirect(Request.UrlReferrer.ToString());
        }
        return RedirectToAction("Index", "Shop");
    }
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarSucursal(Guid idClinica)
        {
            var userId = GetUserId();
            using (var db = new ConexionModel())
            {
                // Buscamos SOLO la clínica que queremos borrar y que nos pertenezca
                var clinicaABorrar = db.clinicas.FirstOrDefault(c => c.id == idClinica && c.usuario_id == userId);
                
                if (clinicaABorrar != null)
                {
                    db.clinicas.Remove(clinicaABorrar);
                    db.SaveChanges();
                    
                    // Limpiamos la sesión porque esa clínica ya no existe
                    Session.Remove("ClinicaActivaId");

                    // Revisamos si le quedan más clínicas
                    var leQuedan = db.clinicas.Any(c => c.usuario_id == userId);
                    if (!leQuedan)
                    {
                        // Si era la única que tenía, lo mandamos a crear una nueva a la fuerza
                        return RedirectToAction("Nueva");
                    }
                }
                
                TempData["Exito"] = "Sucursal eliminada correctamente.";
                return RedirectToAction("MiPerfil");
            }
        }
    }
}