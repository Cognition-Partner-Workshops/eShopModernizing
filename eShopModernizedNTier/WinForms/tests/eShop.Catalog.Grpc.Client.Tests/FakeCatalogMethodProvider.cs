using Grpc.AspNetCore.Server.Model;
using Grpc.Core;

namespace eShop.Catalog.Grpc.Client.Tests;

public sealed class FakeCatalogMethodProvider(FakeCatalogService service) : IServiceMethodProvider<FakeCatalogService>
{
    public void OnServiceMethodDiscovery(ServiceMethodProviderContext<FakeCatalogService> context) =>
        FakeCatalogService.BindService(new Binder(context), service);

    private sealed class Binder(ServiceMethodProviderContext<FakeCatalogService> context) : ServiceBinderBase
    {
        public override void AddMethod<TRequest, TResponse>(Method<TRequest, TResponse> method, UnaryServerMethod<TRequest, TResponse>? handler)
        {
            context.AddUnaryMethod(method, [], (_, request, callContext) => handler!(request, callContext));
        }
    }
}
