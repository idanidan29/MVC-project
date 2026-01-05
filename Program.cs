using MVC_project.Data;
using MVC_project.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Authentication (cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.Cookie.Name = "MvcProjectAuth";
    });

// Increase multipart/form-data upload limit to support large image uploads
builder.Services.Configure<FormOptions>(o =>
{
    // 100 MB total multipart body limit
    o.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});

// If hosting with Kestrel directly, also raise the max request body size
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<TripRepository>();
builder.Services.AddScoped<TripImageRepository>();
builder.Services.AddScoped<UserTripRepository>();
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<TripDateRepository>();
builder.Services.AddScoped<WaitlistRepository>();
builder.Services.AddScoped<TripRatingRepository>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<WaitlistService>();

// Background Services
builder.Services.AddHostedService<WaitlistExpirationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Default route - Info page is now the main landing page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Info}/{action=Index}/{id?}");

app.Run();
