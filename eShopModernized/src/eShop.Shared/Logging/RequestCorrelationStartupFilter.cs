using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace eShop.Shared.Logging;

/// <summary>
/// Inserts <see cref="RequestCorrelationMiddleware"/> at the head of the pipeline so hosts only need
/// the single observability registration call.
/// </summary>
internal sealed class RequestCorrelationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            app.UseMiddleware<RequestCorrelationMiddleware>();
            next(app);
        };
    }
}
