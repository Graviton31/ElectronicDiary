using ElectronicDiaryApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавление сервисов
builder.Services.AddControllersWithViews(); // Поддержка MVC
builder.Services.AddHttpClient(); // Для HttpClientFactory
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Настройка CORS (важно для доступа к API)
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

var app = builder.Build();

// 3. Конфигурация Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFrontend"); // Подключение CORS ПОСЛЕ UseRouting
app.UseAuthorization();
app.UseSession();

// 4. Настройка маршрутов
app.UseEndpoints(endpoints =>
{
    // Для атрибутной маршрутизации (важно для вашего GroupScheduleController)
    endpoints.MapControllers();

    // Conventional routing
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Schedule}/{action=Index}/{id?}");
});

app.Run();