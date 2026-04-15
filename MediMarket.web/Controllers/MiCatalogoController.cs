using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;

namespace MediMarket.web.Controllers
{
    public class MiCatalogoController : BaseProveedorController
    {
        // GET: MiCatalogo
        public ActionResult Index()
        {
            var identity = (ClaimsIdentity)User.Identity;

            ViewBag.Nombre = identity.FindFirst(ClaimTypes.Name)?.Value;
            ViewBag.FotoUrl = identity.FindFirst("foto_url")?.Value;
            return View();
        }
    }
}