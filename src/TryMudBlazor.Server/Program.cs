using Microsoft.EntityFrameworkCore;
using MudBlazor.Examples.Data;
using Try.Core;
using TryMudBlazor.Client;
using TryMudBlazor.Server.Data;
using TryMudBlazor.Server.Services;

namespace TryMudBlazor.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddScoped<IPeriodicTableService, PeriodicTableService>();

        builder.Services.AddDbContextFactory<ApplicationDbContext>(cfg => cfg.UseNpgsql(connString));

        builder.Services.AddSingleton<ComponentService>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://www.mudblazor.com", "https://mudblazor.com");
            });
        });
        builder.Services.AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors();

        // Needed for wasm project
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapControllers();

        // Cache busting
        var cacheBusting = DateTimeVersion.Cache;
        // Custom middleware to replace #{CACHE_TOKEN}# doesn't work in docker
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/index.html" || context.Request.Path == "/") // Check if the request is for index.html or root
            {
                string fileContent = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "..", "TryMudBlazor.Client", "wwwroot", "index.html"));
                string modifiedContent = fileContent.Replace("#{CACHE_TOKEN}#", cacheBusting);

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(modifiedContent);
            }
            else
            {
                await next();
            }
        });        
        app.MapFallbackToFile("index.html").CacheOutput(policy => policy.SetVaryByQuery("cachebust", cacheBusting));

        app.Run();
    }
}