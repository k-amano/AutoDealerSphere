using AutoDealerSphere.Server.Middleware;
using AutoDealerSphere.Server.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

// Syncfusionライセンスキーを設定
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdec3VUR2ddV0V+WkpWYEk=");

WebApplicationBuilder builder;
string connectionString;
string logFolder;

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    // デバッグ時は元の動作（appsettings.json の接続文字列、ContentRootはデフォルト）
    builder = WebApplication.CreateBuilder(args);
    connectionString = builder.Configuration.GetConnectionString("crm01")!;
    logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
}
else
{
    // サービス起動時は ProgramData を使う（全アカウントが書き込み可能）
    var exeFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
    var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AutoDealerSphere");
    Directory.CreateDirectory(dataFolder);
    connectionString = $"Data Source={Path.Combine(dataFolder, "crm01.db")}";
    logFolder = Path.Combine(dataFolder, "logs");

    builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = exeFolder,
    });
}

Directory.CreateDirectory(logFolder);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logFolder, "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Windowsサービスとして動作する場合の対応
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
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
    var dbInitLogger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseInitializeService>>();

    try
    {
        var initializeService = new DatabaseInitializeService(dbContext, dbInitLogger);
        initializeService.Initialize();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "データベースの初期化に失敗しました");
        throw;
    }
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
