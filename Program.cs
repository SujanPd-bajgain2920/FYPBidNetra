using FYPBidNetra.Models;
using FYPBidNetra.Security;
using FYPBidNetra.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

namespace FYPBidNetra
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<EmailService>();

            builder.Services.AddHttpClient();

            // Add DbContext with connection string
            builder.Services.AddDbContext<FypContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("dbConn"))
                       .EnableSensitiveDataLogging());

            // Register DataSecurityProvider
            builder.Services.AddSingleton<DataSecurityProvider>();

            // Add SignalR
            builder.Services.AddSignalR();  // Adding SignalR service

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(o => o.LoginPath = "/Account/Login"); // o=>o. is lamda expression
            // session add
            builder.Services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromMinutes(1);
                o.Cookie.HttpOnly = true;
            });

            builder.Services.AddHttpContextAccessor();
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FlaskAPI",
                    policy =>
                    {
                        policy.WithOrigins("http://127.0.0.1:5000")
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
            });

            var app = builder.Build();


            // Configure Rotativa (PDF generator)
            RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors("FlaskAPI");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // Configure SignalR endpoint
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Static}/{action=Index}/{id?}");

                // Map the SignalR hub route
               endpoints.MapHub<ChatHub>("/chathub");
                endpoints.MapHub<AuctionHub>("/auctionhub");
            });

            app.Run();
        }
    }
}
