using ElectronicDiaryApi.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавление сервисов
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Настройка CORS должна быть ЗДЕСЬ, до builder.Build()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7012", // Ваш MVC-фронтенд
            "https://localhost:7123" // API (если нужно)
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build(); // После этого точка нельзя изменять builder.Services

// 2. Конфигурация Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFrontend"); // Используем политику CORS
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// 3. Настройка маршрутов
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Schedule}/{action=Index}/{id?}");
});

app.Run();