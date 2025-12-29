using System.Text;
using Microsoft.EntityFrameworkCore;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.EFMemeberModels;
using Reservation.Middleware;

namespace Reservation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 設定編碼為 UTF-8
             Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            builder.Services.Configure<RequestLocalizationOptions>(options =>{
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("zh-TW");
            });
            
            // 註冊 Entity Framework Contexts 到依賴注入容器
            builder.Services.AddDbContext<RestaurantContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("RestaurantConnection")));
            
            builder.Services.AddDbContext<MemberContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MemberConnection")));
            
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            
            // 註冊 HttpClient Factory（InlineAppsService 需要）
            builder.Services.AddHttpClient();
            
            // 註冊服務
            builder.Services.AddScoped<Reservation.Service.InlineAppsService>();
            builder.Services.AddScoped<Reservation.Service.RestaurantService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseTokenValidation();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Restaurant}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
