using AutoDealerSphere.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("crm01");

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContextFactory<SQLDBContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IClientService, ClientService>();

var app = builder.Build();

// データベースの作成と初期化
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SQLDBContext>();
    
    // 新しいスキーマでデータベースを確実に作成
    dbContext.Database.EnsureDeleted(); // 既存のデータベースを削除
    dbContext.Database.EnsureCreated(); // 新しいスキーマでデータベースを作成
    
    // サンプルデータの初期化
    DbInitializer.InitializeSampleData(dbContext);
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
