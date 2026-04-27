using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using MediMarket.web.Models;


namespace MediMarket.web.Controllers
{
    public class ProveedoresController : BaseProveedorController
    {
        public ActionResult Index()
        {
            var identity = (ClaimsIdentity)User.Identity;

            ViewBag.Nombre = identity.FindFirst(ClaimTypes.Name)?.Value;
            ViewBag.FotoUrl = identity.FindFirst("foto_url")?.Value;
            ViewBag.Correo = identity.FindFirst(ClaimTypes.Email);
            return View();
        }

         public ActionResult RFQs()
        {
            return RedirectToAction("Index", "SolicitudesRfq");
        }
    }
}