// Controllers/BaseProveedorController.cs
using System.Security.Claims;
using System.Web.Mvc;
using MediMarket.web.Models;

[Authorize]
public class BaseProveedorController : Controller
{
    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var identity = (ClaimsIdentity)User.Identity;
        ViewBag.Nombre = identity.FindFirst(ClaimTypes.Name)?.Value;
        ViewBag.FotoUrl = identity.FindFirst("foto_url")?.Value;
        ViewBag.TipoUsuario = identity.FindFirst("tipo_usuario")?.Value;
        base.OnActionExecuting(filterContext);
    }
}