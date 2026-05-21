using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopPorted.Models;
using eShopPorted.Modules;
using Microsoft.EntityFrameworkCore;

namespace eShopPorted
{
    public class Program
    {
        public static DateTime StartTime { get; } = DateTime.UtcNow;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            builder.Services.AddControllersWithViews();

            bool useMockData = builder.Configuration.GetValue<bool>("UseMockData");
            if (!useMockData)
            {
                string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddDbContext<CatalogDBContext>(options =>
                    options.UseSqlServer(connectionString));
            }

            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterModule(new ApplicationModule(useMockData));
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllerRoute("default", "{controller=Catalog}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
