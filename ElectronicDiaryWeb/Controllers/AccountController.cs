using ElectronicDiaryWeb.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public AccountController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {

        if (User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync($"{_config["ApiBaseUrl"]}/api/auth/login", new
        {
            model.Login,
            model.Password
        });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Неверный логин или пароль");
            return View(model);
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
        SetAuthCookies(result);

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["_secure_at"]);
        await client.PostAsync($"{_config["ApiBaseUrl"]}/api/auth/revoke-token", null);

        ClearAuthCookies();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Schedule");
    }

    private void SetAuthCookies(AuthResponseModel authResult)
    {
        // Храним access-token под неочевидным именем
        Response.Cookies.Append("_secure_at", authResult.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = authResult.AccessTokenExpires,
            IsEssential = true
        });

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(authResult.AccessToken);
        var claims = jwt.Claims.ToList();

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("_secure_at", new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Schedule");
    }
}