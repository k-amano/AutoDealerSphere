using AutoDealerSphere.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("crm01");

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContextFactory<SQLDBContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IVehicleImportService, VehicleImportService>();

var app = builder.Build();

// データベースの作成と初期化
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SQLDBContext>();
    
    try
    {
        // データベースが存在しない場合は作成
        dbContext.Database.EnsureCreated();
        
        // Vehiclesテーブルが存在しない場合は作成
        var tableExists = dbContext.Database.ExecuteSqlRaw(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Vehicles'") > 0;
        
        if (!tableExists)
        {
            // Vehiclesテーブルを作成
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Vehicles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClientId INTEGER NOT NULL,
                    LicensePlateLocation TEXT,
                    LicensePlateClassification TEXT,
                    LicensePlateHiragana TEXT,
                    LicensePlateNumber TEXT,
                    KeyNumber TEXT,
                    ChassisNumber TEXT,
                    TypeCertificationNumber TEXT,
                    CategoryNumber TEXT,
                    VehicleName TEXT,
                    VehicleModel TEXT,
                    Mileage DECIMAL(18,2),
                    FirstRegistrationDate TEXT,
                    Purpose TEXT,
                    PersonalBusinessUse TEXT,
                    BodyShape TEXT,
                    SeatingCapacity INTEGER,
                    MaxLoadCapacity INTEGER,
                    VehicleWeight INTEGER,
                    VehicleTotalWeight INTEGER,
                    VehicleLength INTEGER,
                    VehicleWidth INTEGER,
                    VehicleHeight INTEGER,
                    FrontOverhang INTEGER,
                    RearOverhang INTEGER,
                    ModelCode TEXT,
                    EngineModel TEXT,
                    Displacement DECIMAL(18,2),
                    FuelType TEXT,
                    InspectionExpiryDate TEXT,
                    NextInspectionDate TEXT,
                    InspectionCertificateNumber TEXT,
                    UserNameOrCompany TEXT,
                    UserAddress TEXT,
                    BaseLocation TEXT,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                    UpdatedAt TEXT,
                    FOREIGN KEY (ClientId) REFERENCES Clients(Id) ON DELETE CASCADE
                )
            ");
        }
        
        // 初回起動時のみサンプルデータを初期化（Usersテーブルにデータがない場合）
        if (!dbContext.Users.Any())
        {
            DbInitializer.InitializeSampleData(dbContext);
        }
    }
    catch (Exception ex)
    {
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
