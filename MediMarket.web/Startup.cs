using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using System.Web.Helpers; // Para AntiForgeryConfig
using System.Security.Claims; // Para ClaimTypes
using Owin;
using System.Configuration;

[assembly: OwinStartup(typeof(MediMarket.Startup))] 
namespace MediMarket
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
{
    AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

    // 1. Configurar la autenticación por Cookies
    app.UseCookieAuthentication(new CookieAuthenticationOptions
    {
        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
        LoginPath = new PathString("/Account/Login") 
    });

    // 2. Configurar la Cookie Externa (La sala de espera)
    app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

    // 3. EL FIX: Dile a Google que deje los datos en la ExternalCookie
    app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ExternalCookie);

    // 4. Configurar Google
    app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
    {
        ClientId = ConfigurationManager.AppSettings["GoogleClientId"],
        ClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"],
        
        Provider = new GoogleOAuth2AuthenticationProvider()
        {
            OnAuthenticated = (context) =>
            {
                var pictureUrl = context.User["picture"]?.ToString();
                if (!string.IsNullOrEmpty(pictureUrl))
                {
                    context.Identity.AddClaim(new Claim("urn:google:picture", pictureUrl));
                }
                return System.Threading.Tasks.Task.FromResult(0);
            }
        }
    });
}
    }
}