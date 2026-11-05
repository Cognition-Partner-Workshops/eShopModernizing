using eShopModernizedWebForms.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ICatalogService, CatalogServiceMock>();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Index", "Default");
    options.Conventions.AddPageRoute("/Index", "Default.aspx");
    options.Conventions.AddPageRoute("/About", "About.aspx");
    options.Conventions.AddPageRoute("/Contact", "Contact.aspx");
    options.Conventions.AddPageRoute("/Index", "Default/index/{index:int}/size/{size:int}");
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
var app = builder.Build();
if (!app.Configuration.GetValue("UseMockData", true))
{
    throw new NotSupportedException("UseMockData=false is not supported by this port. Configure UseMockData=true to use the in-memory catalog.");
}
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.Use(async (context, next) =>
{
    if (context.Session.GetString("SessionStartTime") == null)
    {
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString());
    }
    await next();
});
app.MapRazorPages();
app.Run();

public partial class Program { }
