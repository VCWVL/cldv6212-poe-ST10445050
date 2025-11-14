using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ST10445050_CLDV6212_POE_Part1.Services;
using ST10445050_CLDV6212_POE_Part1.DbContext;

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
builder.Services.AddHttpClient<CustomerService>(); // Registers HttpClient for CustomerService
builder.Services.AddScoped<CustomerService>(); // Add CustomerService as a scoped service

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
// Register Database context (SQL Database) for User login
// ============================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("AzureSQL")));

// ============================
// Register Session services (for login management)
// ============================
builder.Services.AddDistributedMemoryCache();  // Required for session
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ============================
// Register identity-related services
// ============================
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";  // Redirect here when not authenticated
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

// Use session for storing user info
app.UseSession();

// Use Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();  // Ensure the database is up-to-date
}

// Run the application
app.Run();
