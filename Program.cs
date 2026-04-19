using Lab07.Data;
using Lab07.Repositories;
using Lab07.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Lab07.Models;

var builder = WebApplication.CreateBuilder(args);

// Containerul DI (Dependency Injection) este configurat aici, înainte de construirea aplicației.
// Fiecare serviciu este înregistrat cu durata de viață Scoped — o instanță nouă per request HTTP.
// Lanțul de dependențe: Controller → IArticleService/ICategoryService → IUnitOfWork → AppDbContext.
// ASP.NET Core rezolvă automat aceste dependențe prin constructori, fără a fi nevoie de `new`.

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie redirect paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});


// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();


// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
// Ordinea conteaza!
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
