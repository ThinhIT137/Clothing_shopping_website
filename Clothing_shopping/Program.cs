using Clothing_shopping.Hubs;
using Clothing_shopping.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

builder.Services.AddDbContext<ClothingContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("clothingDB"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//    endpoints.MapHub<AppHub>("/appHub");
//});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=User}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ClothingContext>();
        // Th?c hi?n m?t truy v?n nh? nhàng ?? kh?i t?o k?t n?i và model
        _ = await context.Users.FirstOrDefaultAsync();
        Console.WriteLine(">>> Application has been warmed up. Database connection is ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("An error occurred during DB warming up. " + ex.Message);
    }
}

//app.MapHub<AppHub>("/appHub");
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
