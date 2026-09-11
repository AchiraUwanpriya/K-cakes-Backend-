using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http.Cors;

namespace ClassSystemAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // SHOW REAL ERRORS 
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            // Enable CORS - Fixed the syntax
            var cors = new EnableCorsAttribute(
                "http://localhost:3000,https://sweetofkcake.dtselife.com", 
                "*",  
                "*"   
            );
            config.EnableCors(cors);

            // JSON formatting
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling =
                ReferenceLoopHandling.Ignore;

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();

            // Attribute routing
            config.MapHttpAttributeRoutes();

            // Default route
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}