namespace TryMudBlazor.Client
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Blazored.LocalStorage;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
    using Microsoft.AspNetCore.Components.WebAssembly.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.JSInterop;
    using MudBlazor.Services;
    using Services.UserPreferences;
    using Try.Core;
    using Try.UserComponents;
    using TryMudBlazor.Client.Models;
    using TryMudBlazor.Client.Services;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddSingleton(serviceProvider => (IJSInProcessRuntime)serviceProvider.GetRequiredService<IJSRuntime>());
            builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<SnippetsService>();
            builder.Services.AddSingleton(new CompilationService());

            builder.Services
                .AddOptions<SnippetsOptions>()
                .Configure<IConfiguration>((options, configuration) => configuration.GetSection("Snippets").Bind(options));

            builder.Logging.Services.AddSingleton<ILoggerProvider, HandleCriticalUserComponentExceptionsLoggerProvider>();
            builder.Services.AddMudServices();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
            builder.Services.AddScoped<LayoutService>();            

            var jsRuntime = GetJsRuntime();
            try
            {
                await jsRuntime.InvokeVoidAsync("Try.StaticAssets.loadStaticAssets");

                ExecuteUserDefinedConfiguration(builder);
            }
            catch (Exception exception)
            {
                // We shouldn't throw during app start so just give the user the info that an exception has been thrown,
                // update the user components DLL to make sure the app will run on reload and continue the app execution
                var actualException = exception is TargetInvocationException tie ? tie.InnerException : exception;
                await Console.Error.WriteLineAsync($"Error on app startup: {actualException}");

                jsRuntime.InvokeVoid(Try.CodeExecution.UpdateUserComponentsDll, CoreConstants.DefaultUserComponentsAssemblyBytes);
            }

            await builder.Build().RunAsync();
        }

        private static void ExecuteUserDefinedConfiguration(WebAssemblyHostBuilder builder)
        {
            var userComponentsAssembly = typeof(__Main).Assembly;
            var startupType = userComponentsAssembly.GetType("UserStartup", throwOnError: false, ignoreCase: true)
                              ?? userComponentsAssembly.GetType("Try.UserComponents.UserStartup", throwOnError: false, ignoreCase: true);
            if (startupType == null)
                return;
            var configureMethod = startupType.GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);
            if (configureMethod == null)
                return;
            var configureMethodParams = configureMethod.GetParameters();
            if (configureMethodParams.Length != 1 || configureMethodParams[0].ParameterType != typeof(WebAssemblyHostBuilder))
                return;
            configureMethod.Invoke(obj: null, new object[] { builder });
        }

        private static IJSInProcessRuntime GetJsRuntime()
        {
            const string defaultJsRuntimeTypeName = "DefaultWebAssemblyJSRuntime";
            const string instanceFieldName = "Instance";

            var defaultJsRuntimeType = typeof(LazyAssemblyLoader).Assembly
                .GetTypes()
                .SingleOrDefault(t => t.Name == defaultJsRuntimeTypeName);

            if (defaultJsRuntimeType == null)
            {
                throw new MissingMemberException($"Couldn't find type '{defaultJsRuntimeTypeName}'.");
            }

            var instanceField = defaultJsRuntimeType.GetField(instanceFieldName, BindingFlags.Static | BindingFlags.Public);
            if (instanceField == null)
            {
                throw new MissingMemberException($"Couldn't find property '{instanceFieldName}' in '{defaultJsRuntimeTypeName}'.");
            }

            return (IJSInProcessRuntime)instanceField.GetValue(obj: null);
        }
    }
}
