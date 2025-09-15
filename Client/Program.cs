using AutoDealerSphere.Client;
using AutoDealerSphere.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;


Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdec3VUR2ddV0V+WkpWYEk=");
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddSyncfusionBlazor();

await builder.Build().RunAsync();
