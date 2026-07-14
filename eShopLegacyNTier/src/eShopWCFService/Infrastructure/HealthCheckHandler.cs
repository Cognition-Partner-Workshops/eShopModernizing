using System.Web;

namespace eShopWCFService.Infrastructure
{
    /// <summary>
    /// Lightweight liveness endpoint served at GET /health. It is intentionally
    /// free of database or WCF dependencies so container orchestrators can probe
    /// the process without touching application state.
    /// </summary>
    public class HealthCheckHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Clear();
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = 200;
            context.Response.Write("Healthy");
        }
    }
}
