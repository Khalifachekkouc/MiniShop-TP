using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Services;

// Model binding parses "13999.50" (what <input type="number"> posts) the same way on every machine.
// Prices are displayed in French format by MoneyFormat, independently of this.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST/PUT/DELETE needs a valid anti-forgery token, even if an action forgets the attribute.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // French messages for values that cannot be parsed at all (e.g. letters in a number field).
    var m = options.ModelBindingMessageProvider;
    m.SetValueMustNotBeNullAccessor(_ => "Ce champ est obligatoire.");
    m.SetAttemptedValueIsInvalidAccessor((value, field) => $"« {value} » n'est pas une valeur valide pour {field}.");
    m.SetValueIsInvalidAccessor(value => $"« {value} » n'est pas une valeur valide.");
    m.SetMissingKeyOrValueAccessor(() => "Ce champ est obligatoire.");
    m.SetValueMustBeANumberAccessor(field => $"{field} doit être un nombre.");
    m.SetNonPropertyValueMustBeANumberAccessor(() => "La valeur doit être un nombre.");
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSeeding((context, _) => DbSeeder.Seed(context))
           .UseAsyncSeeding((context, _, ct) => DbSeeder.SeedAsync(context, ct)));
builder.Services.AddSingleton<ImageStorage>();

// Session-based cart: the cookie only carries a session id; the cart lives in server memory.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".MiniShop.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(2);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CategoryMenu>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Applies pending migrations (and the demo seed) so a fresh clone runs with one command.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");
app.UseHttpsRedirection();

// Serves images uploaded at runtime: MapStaticAssets only knows files that existed at build time.
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

// Admin categories: /admin/categories, /admin/categories/Edit/2 ... (before the catalogue route,
// which would read "categories" as one of its own actions).
app.MapAreaControllerRoute(
    name: "admin-categories",
    areaName: "Admin",
    pattern: "admin/categories/{action=Index}/{id?}",
    defaults: new { controller = "Categories" })
    .WithStaticAssets();

// Admin dashboard: /admin, /admin/Create, /admin/Edit/5 ... (Areas/Admin).
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "admin/{action=Index}/{id?}",
    defaults: new { controller = "Produits" })
    .WithStaticAssets();

// Storefront pages use attribute routes: /, /produit/{id}, /panier, /paiement.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shop}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
