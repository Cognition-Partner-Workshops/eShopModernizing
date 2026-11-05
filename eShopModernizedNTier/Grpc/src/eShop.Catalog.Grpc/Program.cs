using eShop.Catalog.Data;
using eShop.Catalog.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEShopCatalogWithStockServices(builder.Configuration);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();
app.MapGrpcService<CatalogGrpcService>();
app.MapGrpcReflectionService();
app.Run();

public partial class Program;
