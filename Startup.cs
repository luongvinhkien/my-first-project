using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private string ConvertPostgresUrlToConnectionString(string url)
        {
            var uri = new Uri(url);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<RestaurantDbContext>(options =>
            {
                var pgConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
                
                Console.WriteLine("==================================================");
                Console.WriteLine($"[STARTUP] DATABASE_URL found: {!string.IsNullOrEmpty(pgConnectionString)}");
                if (!string.IsNullOrEmpty(pgConnectionString))
                {
                    var secureUrl = pgConnectionString.Split('@').LastOrDefault();
                    Console.WriteLine($"[STARTUP] DATABASE_URL (host part): {secureUrl}");
                    
                    if (pgConnectionString.StartsWith("postgres://") || pgConnectionString.StartsWith("postgresql://"))
                    {
                        pgConnectionString = ConvertPostgresUrlToConnectionString(pgConnectionString);
                    }
                    options.UseNpgsql(pgConnectionString);
                }
                else
                {
                    Console.WriteLine("[STARTUP] DATABASE_URL is missing. Falling back to SQL Server.");
                    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
                }
                Console.WriteLine("==================================================");
            });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
