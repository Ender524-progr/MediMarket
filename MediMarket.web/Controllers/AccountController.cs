using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.Cookies;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security.Google;
using System.Security.Claims;
using MediMarket.web.Models;

namespace MediMarket.web.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void LoginConGoogle()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse", "Account") },
                "Google"
            );
        }

        [AllowAnonymous]
        public async Task<ActionResult> GoogleResponse()
        {
            var loginInfo = await HttpContext.GetOwinContext().Authentication.GetExternalLoginInfoAsync();
            if (loginInfo == null) return RedirectToAction("Login");

            string email = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Email);
            string nombreCompleto = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Name);
            string foto = loginInfo.ExternalIdentity.FindFirstValue("urn:google:picture");

            using (var db = new ConexionModel())
            {
                var usuarioExistente = db.usuarios.FirstOrDefault(u => u.email == email);
                if (usuarioExistente != null)
                {
                    SignInUser(usuarioExistente); // ✅ firmamos sesión del usuario existente
                    return RedirectToAction("Index", "Proveedores");
                }
                else
                {
                    return RedirectToAction("CompletarPerfil", "Account", new { correo = email, nombre = nombreCompleto, fotoUrl = foto });
                }
            }
        }

        public ActionResult CompletarPerfil(string correo, string nombre, string fotoUrl)
        {
            ViewBag.Correo = correo;
            ViewBag.Nombre = nombre;
            ViewBag.FotoUrl = fotoUrl;
            return View();
        }

        [HttpPost]
        public ActionResult RegistrarClinica(
            string email, string nombre, string fotoUrl,
            string nombreClinica, string rfc, string cedula, string especialidad, string telefono,
            string cp, string estado, string municipio, string colonia, string calle, string num_ext, string num_int)
        {
            using (var db = new ConexionModel())
            {
                var user = new usuarios
                {
                    id = Guid.NewGuid(),
                    email = email,
                    nombre = nombre,
                    foto_url = fotoUrl,
                    creado_en = DateTime.Now,
                    tipo_usuario = "clinica",
                    estado_verificacion = "pendiente"
                };
                db.usuarios.Add(user);

                string direccionArmada = $"{calle} {num_ext}";
                if (!string.IsNullOrWhiteSpace(num_int)) direccionArmada += $" Int. {num_int}";
                direccionArmada += $", Col. {colonia}";

                db.clinicas.Add(new clinicas
                {
                    id = Guid.NewGuid(),
                    usuario_id = user.id,
                    nombre_clinica = nombreClinica,
                    rfc = rfc,
                    cedula_profesional = cedula,
                    especialidad = especialidad,
                    telefono = telefono,
                    cp = cp,
                    estado = estado,
                    ciudad = municipio,
                    direccion = direccionArmada
                });

                db.SaveChanges();
                SignInUser(user); // ✅
                return RedirectToAction("Index", "Shop");
            }
        }

        [HttpPost]
        public ActionResult RegistrarProveedor(
            string email, string nombre, string fotoUrl,
            string nombreEmpresa, string rfc, string telefono, string categoria, string registroSanitario,
            string cp, string estado, string municipio, string colonia, string calle, string num_ext, string num_int)
        {
            using (var db = new ConexionModel())
            {
                var user = new usuarios
                {
                    id = Guid.NewGuid(),
                    email = email,
                    nombre = nombre,
                    foto_url = fotoUrl,
                    creado_en = DateTime.Now,
                    tipo_usuario = "proveedor",
                    estado_verificacion = "pendiente"
                };
                db.usuarios.Add(user);

                string direccionArmada = $"{calle} {num_ext}";
                if (!string.IsNullOrWhiteSpace(num_int)) direccionArmada += $" Int. {num_int}";
                direccionArmada += $", Col. {colonia}, {municipio}, {estado}";

                db.proveedores.Add(new proveedores
                {
                    id = Guid.NewGuid(),
                    usuario_id = user.id,
                    nombre_empresa = nombreEmpresa,
                    rfc = rfc,
                    telefono = telefono,
                    categoria_principal = categoria,
                    registro_sanitario = registroSanitario,
                    cp = cp,
                    direccion = direccionArmada
                });

                db.SaveChanges();
                SignInUser(user); // ✅
                return RedirectToAction("Index", "Proveedores");
            }
        }

        [HttpPost]
        public ActionResult RegistrarHibrido(
            string email, string nombre, string fotoUrl,
            string cedula, string especialidad, string nombreEmpresa, string rfc, string telefono, string categoria, string registroSanitario,
            string cp, string estado, string municipio, string colonia, string calle, string num_ext, string num_int)
        {
            using (var db = new ConexionModel())
            {
                var user = new usuarios
                {
                    id = Guid.NewGuid(),
                    email = email,
                    nombre = nombre,
                    foto_url = fotoUrl,
                    creado_en = DateTime.Now,
                    tipo_usuario = "hibrido",
                    estado_verificacion = "pendiente"
                };
                db.usuarios.Add(user);

                string direccionClinica = $"{calle} {num_ext}";
                if (!string.IsNullOrWhiteSpace(num_int)) direccionClinica += $" Int. {num_int}";
                direccionClinica += $", Col. {colonia}";

                string direccionProveedor = direccionClinica + $", {municipio}, {estado}";

                db.clinicas.Add(new clinicas
                {
                    id = Guid.NewGuid(),
                    usuario_id = user.id,
                    nombre_clinica = nombreEmpresa,
                    rfc = rfc,
                    cedula_profesional = cedula,
                    especialidad = especialidad,
                    telefono = telefono,
                    cp = cp,
                    estado = estado,
                    ciudad = municipio,
                    direccion = direccionClinica
                });

                db.proveedores.Add(new proveedores
                {
                    id = Guid.NewGuid(),
                    usuario_id = user.id,
                    nombre_empresa = nombreEmpresa,
                    rfc = rfc,
                    telefono = telefono,
                    categoria_principal = categoria,
                    registro_sanitario = registroSanitario,
                    cp = cp,
                    direccion = direccionProveedor
                });

                db.SaveChanges();
                SignInUser(user); // ✅
                return RedirectToAction("Index", "Proveedores");
            }
        }

        public ActionResult Logout()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login");
        }

        private void SignInUser(usuarios user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                new Claim(ClaimTypes.Name,           user.nombre),
                new Claim(ClaimTypes.Email,          user.email),
                new Claim("foto_url",                user.foto_url ?? ""),
                new Claim("tipo_usuario",            user.tipo_usuario)
            };

            var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);

            HttpContext.GetOwinContext().Authentication.SignIn(
                new AuthenticationProperties { IsPersistent = true },
                identity
            );
        }
    }
}