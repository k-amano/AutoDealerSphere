using AutoDealerSphere.Client;
using AutoDealerSphere.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;


Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXpedHRWRmZfVUd1X0BWYEo=");
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped<ErrorService>();
builder.Services.AddScoped(sp =>
    new ClientLogService(baseAddress, sp.GetRequiredService<NavigationManager>()));
builder.Services.AddScoped(sp =>
{
    var errorService = sp.GetRequiredService<ErrorService>();
    var clientLogService = sp.GetRequiredService<ClientLogService>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var handler = new ErrorHandlingHttpHandler(errorService, clientLogService, navigationManager)
    {
        InnerHandler = new HttpClientHandler()
    };
    return new HttpClient(handler) { BaseAddress = baseAddress };
});
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddSyncfusionBlazor();

await builder.Build().RunAsync();
