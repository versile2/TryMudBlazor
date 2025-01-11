using Microsoft.EntityFrameworkCore;
using MudBlazor.Examples.Data;
using TryMudBlazor.Server.Data;

namespace TryMudBlazor.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddScoped<IPeriodicTableService, PeriodicTableService>();

        builder.Services.AddDbContextFactory<ApplicationDbContext>(cfg => cfg.UseNpgsql(connString));

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
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}