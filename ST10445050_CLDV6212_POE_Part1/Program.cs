using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ST10445050_CLDV6212_POE_Part1.Services;

// ST10445050 
// KEONA MACKAN
// CLDV6212 POE PART 2

var builder = WebApplication.CreateBuilder(args);

// ============================
// Add services to the container
// ============================
builder.Services.AddControllersWithViews();

// Get configuration
var configuration = builder.Configuration;

// ✅ Register BlobService
builder.Services.AddSingleton<BlobService>();

// ✅ Register TableStorageService
builder.Services.AddSingleton<TableService>(sp =>
{
    var connectionString = configuration.GetConnectionString("AzureStorage");
    return new TableService(connectionString);
});

// ✅ Register CustomerService & ProductService
builder.Services.AddHttpClient<CustomerService>();
builder.Services.AddScoped<CustomerService>();

builder.Services.AddHttpClient<ProductService>();
builder.Services.AddScoped<ProductService>();

// ============================
// Register OrderService & QueueService
// ============================
builder.Services.AddSingleton<QueueService>(sp =>
{
    var connectionString = configuration.GetConnectionString("AzureStorage");
    return new QueueService(connectionString, "order-messages");
});

// ✅ UploadsService
builder.Services.AddHttpClient<UploadsService>();
builder.Services.AddScoped<UploadsService>();

// ✅ FileStorageService
builder.Services.AddSingleton<FileStorageService>(sp =>
{
    var connectionString = configuration.GetConnectionString("AzureStorage");
    return new FileStorageService(connectionString, "uploads");
});

// ============================
// Build the app
// ============================
var app = builder.Build();

// ============================
// Configure the HTTP request pipeline
// ============================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var tableService = scope.ServiceProvider.GetRequiredService<TableService>();
    await tableService.SeedInitialDataAsync();
}

app.Run();
