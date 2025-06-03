using ElectronicDiaryApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ���������� ��������
builder.Services.AddControllersWithViews(); // ��������� MVC
builder.Services.AddHttpClient(); // ��� HttpClientFactory
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. ��������� CORS (����� ��� ������� � API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7012", // ��� MVC-��������
            "https://localhost:7123" // API (���� �����)
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// 3. ������������ Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFrontend"); // ����������� CORS ����� UseRouting
app.UseAuthorization();
app.UseSession();

// 4. ��������� ���������
app.UseEndpoints(endpoints =>
{
    // ��� ���������� ������������� (����� ��� ������ GroupScheduleController)
    endpoints.MapControllers();

    // Conventional routing
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Schedule}/{action=Index}/{id?}");
});

app.Run();