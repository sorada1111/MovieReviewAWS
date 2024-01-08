using _301270677_301289381_Prathan_VinicioJacome__Lab3.Models;
using Microsoft.EntityFrameworkCore;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Connector;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Repository;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Service;
using Amazon;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;


namespace _301270677_301289381_Prathan_VinicioJacome__Lab3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Set up detailed logging
            builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Trace);


            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(36000); //36000 -> 10hours lifetime
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //DB
            //builder.Services.AddDbContext<MovieContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Connection2RDS")));
            builder.Configuration.AddSystemsManager("/Assignment3Movie", new Amazon.Extensions.NETCore.Setup.AWSOptions { 
            Region=RegionEndpoint.CACentral1});

            var connectionString = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Connection2RDS"));
            connectionString.UserID = builder.Configuration["DbUser"];
            connectionString.Password = builder.Configuration["DbPassword"];
            builder.Services.AddDbContext<MovieContext>(options => options.UseSqlServer(connectionString.ConnectionString));


            // Register repository and service 
            builder.Services.AddScoped<IMovieRepository, MovieRepository>();
            builder.Services.AddScoped<IMovieService, MovieService>();


            // Register AWSConnector service here
            builder.Services.AddSingleton<AWSConnector>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}