using System.Web.Mvc;
using System.Web.Routing;

namespace Paystation
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // checkout page
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            // Used by the browser to poll transaction details
            routes.MapRoute(
                name: "Ajax",
                url: "{controller}/{action}",
                defaults: new { controller = "Home", action = "Ajax" }
            );

            // Paystation POST response
            routes.MapRoute(
                name: "Callback",
                url: "{controller}/{action}",
                defaults: new { controller = "Home", action = "Callback" }
            );
        }
    }
}
