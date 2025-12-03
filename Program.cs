using TourViet.Extensions;
using TourViet.Middleware;
using TourViet.Services;
using TourViet.Services.Interfaces;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        // Allow large form collections (for tours with many itineraries/prices)
        options.MaxModelBindingCollectionSize = 10000; // Default is 1024
    });

// Configure Kestrel for large requests
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Allow larger request bodies (100MB to handle tours with many entries and images)
    serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

// Configure form options for multipart/form-data
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    // Allow larger forms
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
    options.ValueLengthLimit = int.MaxValue;
    options.ValueCountLimit = 10000; // Allow up to 10000 form values
    options.KeyLengthLimit = int.MaxValue;
});

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
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddApplicationServices();

// Add SignalR for real-time features
builder.Services.AddSignalR();


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

// Ensure required infrastructure (Uploads folder, legacy DB cleanup)
app.UseInfrastructureSetup();

app.UseRouting();

// Add Session middleware (must be before UseAuthorization)
app.UseSession();

// Add custom middleware (order matters!)
app.UseMiddleware<GlobalExceptionMiddleware>(); // Should be first to catch all exceptions
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseBookingAuthorization(); // Booking-specific authorization

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map SignalR hubs
app.MapHub<TourViet.Hubs.ReviewHub>("/hubs/review");
app.MapHub<TourViet.Hubs.TourHub>("/hubs/tour");
app.MapHub<TourViet.Hubs.UserHub>("/hubs/user");



app.Run();
