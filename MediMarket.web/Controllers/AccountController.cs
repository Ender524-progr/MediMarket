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

namespace MediMarket.web.Controllers
{
    public class AccountController : Controller
    {
        // 1. Vista de Login
        public ActionResult Login()
        {
            return View();
        }

        // 2. Disparar flujo de Google
        [HttpPost]
        [ValidateAntiForgeryToken]
        public void LoginConGoogle()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse", "Account") },
                "Google"
            );
        }

        // 3. Respuesta de Google
        [AllowAnonymous]
public async Task<ActionResult> GoogleResponse()
{
    var loginInfo = await HttpContext.GetOwinContext().Authentication.GetExternalLoginInfoAsync();
    if (loginInfo == null) return RedirectToAction("Login");

    string email = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Email);
    string nombreCompleto = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Name);
    // Extraemos la URL de la foto que configuramos en el Startup
    string foto = loginInfo.ExternalIdentity.FindFirstValue("urn:google:picture"); 

    using (var db = new ConexionModel())
    {
        var usuarioExistente = db.usuarios.FirstOrDefault(u => u.email == email);
        if (usuarioExistente != null)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        else
        {
            // Pasamos la foto en la redirección
            return RedirectToAction("CompletarPerfil", "Account", new { correo = email, nombre = nombreCompleto, fotoUrl = foto });
        }
    }
}

        // 4. Vista de selección de perfil
        public ActionResult CompletarPerfil(string correo, string nombre, string fotoUrl)
        {
            ViewBag.Correo = correo;
            ViewBag.Nombre = nombre;
            ViewBag.FotoUrl = fotoUrl;
            return View();
        }

// Registrar Clínica
[HttpPost]
public ActionResult RegistrarClinica(
    string email, string nombre, string fotoUrl,
    string nombreClinica, string rfc, string cedula, string especialidad, string telefono, 
    string cp, string estado, string municipio, string colonia, string calle, string num_ext, string num_int) 
{
    using (var db = new ConexionModel())
    {
        // 1. Crear el usuario base
        var user = new usuarios {
            id = Guid.NewGuid(),
            email = email,
            nombre = nombre,
            foto_url = fotoUrl,
            creado_en = DateTime.Now,
            tipo_usuario = "clinica",
            estado_verificacion = "pendiente"
        };
        db.usuarios.Add(user);

        // 2. Armar la dirección completa concatenando los campos
        string direccionArmada = $"{calle} {num_ext}";
        if (!string.IsNullOrWhiteSpace(num_int)) { direccionArmada += $" Int. {num_int}"; }
        direccionArmada += $", Col. {colonia}";

        // 3. Crear el registro de clínica vinculado
        var clinica = new clinicas {
            id = Guid.NewGuid(),
            usuario_id = user.id,
            nombre_clinica = nombreClinica,
            rfc = rfc,
            cedula_profesional = cedula,
            especialidad = especialidad,
            telefono = telefono,
            cp = cp, // Tu BD ya tiene este campo separado
            estado = estado, // Tu BD ya tiene este campo separado
            ciudad = municipio, // Guardamos el municipio en tu campo 'ciudad'
            direccion = direccionArmada // ¡Aquí mandamos el string gigante!
        };
        db.clinicas.Add(clinica);

        db.SaveChanges();
          return Content("¡Registro exitoso! Mi amor cambia esto por el dashboard");
    }
}

// Registrar Proveedor
[HttpPost]
public ActionResult RegistrarProveedor(
    string email, string nombre, string fotoUrl,
    string nombreEmpresa, string rfc, string telefono, string categoria, string registroSanitario,
    string cp, string estado, string municipio, string colonia, string calle, string num_ext, string num_int)
{
    using (var db = new ConexionModel())
    {
        // 1. Crear el usuario base
        var user = new usuarios {
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
        if (!string.IsNullOrWhiteSpace(num_int)) { direccionArmada += $" Int. {num_int}"; }
        direccionArmada += $", Col. {colonia}, {municipio}, {estado}";

        var prov = new proveedores {
            id = Guid.NewGuid(),
            usuario_id = user.id,
            nombre_empresa = nombreEmpresa,
            rfc = rfc,
            telefono = telefono,
            categoria_principal = categoria,
            registro_sanitario = registroSanitario,
            cp = cp,
            direccion = direccionArmada // Guardamos la dirección completa
        };
        db.proveedores.Add(prov);

        db.SaveChanges();
        return Content("¡Registro exitoso! Mi amor cambia esto por el dashboard");
    }
}

// Registrar Híbrido
[HttpPost]
public ActionResult RegistrarHibrido(
    string email, string nombre, string fotoUrl,
    string cedula, string especialidad, string nombreEmpresa, string rfc, string telefono, string categoria, string registroSanitario,
    string cp, string estado, string municipio, string colonia, string calle, string num_ext, string num_int)
{
    using (var db = new ConexionModel())
    {
        // 1. Crear el usuario base
        var user = new usuarios {
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
        if (!string.IsNullOrWhiteSpace(num_int)) { direccionClinica += $" Int. {num_int}"; }
        direccionClinica += $", Col. {colonia}";

        string direccionProveedor = direccionClinica + $", {municipio}, {estado}";

        // 3. Registro de Clínica (Aprovechamos la dirección unificada)
        db.clinicas.Add(new clinicas {
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

        // 4. Registro de Proveedor
        db.proveedores.Add(new proveedores {
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
        return Content("¡Registro Híbrido exitoso! Mi amor cambia esto por el dashboard");
    }
}



        // 7. Cerrar sesión
        public ActionResult Logout()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login");
        }
    }
}