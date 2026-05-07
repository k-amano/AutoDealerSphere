using AutoDealerSphere.Server.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

// Syncfusionライセンスキーを設定
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdec3VUR2ddV0V+WkpWYEk=");

WebApplicationBuilder builder;
string connectionString;

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    // デバッグ時は元の動作（appsettings.json の接続文字列、ContentRootはデフォルト）
    builder = WebApplication.CreateBuilder(args);
    connectionString = builder.Configuration.GetConnectionString("crm01")!;
}
else
{
    // サービス起動時は ProgramData を使う（全アカウントが書き込み可能）
    var exeFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
    var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AutoDealerSphere");
    Directory.CreateDirectory(dbFolder);
    connectionString = $"Data Source={Path.Combine(dbFolder, "crm01.db")}";
    Console.WriteLine($"DB path: {connectionString}");

    builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = exeFolder,
    });
}

// Windowsサービスとして動作する場合の対応
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddRazorPages();
builder.Services.AddDbContextFactory<SQLDBContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDataProtection();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IVehicleImportService, VehicleImportService>();
builder.Services.AddScoped<IPartService, PartService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IStatutoryFeeService, StatutoryFeeService>();
builder.Services.AddScoped<IIssuerInfoService, IssuerInfoService>();
builder.Services.AddScoped<IDataManagementService, DataManagementService>();
builder.Services.AddScoped<IEmailSettingsService, EmailSettingsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<JwtService>();

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
