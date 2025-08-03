using AutoDealerSphere.Server.Services;
using Microsoft.EntityFrameworkCore;

// Syncfusionライセンスキーを設定
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdec3VUR2ddV0V+WkpWYEk=");

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("crm01");

// Add services to the container.

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddRazorPages();
builder.Services.AddDbContextFactory<SQLDBContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IVehicleImportService, VehicleImportService>();
builder.Services.AddScoped<IPartService, PartService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

var app = builder.Build();

// データベースの初期化
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SQLDBContext>();
    
    try
    {
        var initializeService = new DatabaseInitializeService(dbContext);
        initializeService.Initialize();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();