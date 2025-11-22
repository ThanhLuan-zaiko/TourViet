using TourViet.Extensions;
using TourViet.Middleware;
using TourViet.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews();

builder.Services
    .AddPersistence(builder.Configuration);

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Register Services
builder.Services.AddMemoryCache(); // For rate limiting
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPasswordStrengthValidator, PasswordStrengthValidator>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddApplicationServices();

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

// Serve files from Uploads folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
    RequestPath = "/Uploads"
});

app.UseRouting();

// Add Session middleware (must be before UseAuthorization)
app.UseSession();

// Add custom middleware (order matters!)
app.UseMiddleware<GlobalExceptionMiddleware>(); // Should be first to catch all exceptions
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
