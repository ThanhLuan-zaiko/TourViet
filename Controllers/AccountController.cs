using Microsoft.AspNetCore.Mvc;
using TourViet.DTOs.Responses;
using TourViet.Extensions;
using TourViet.Services;
using TourViet.ViewModels;

namespace TourViet.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (IsUserLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = model.ToRequest();
        var response = await _authService.LoginAsync(request);

        if (!response.Success || response.User == null)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View(model);
        }

        // Store user info in session
        SetUserSession(response.User);

        if (model.RememberMe)
        {
            Response.Cookies.Append("RememberMe", "true", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }

        _logger.LogInformation("User {Email} logged in successfully", response.User.Email);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (IsUserLoggedIn())
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = model.ToRequest();
        var response = await _authService.RegisterAsync(request);

        if (!response.Success || response.User == null)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View(model);
        }

        // Auto login after registration
        SetUserSession(response.User);

        _logger.LogInformation("User {Email} registered successfully", response.User.Email);

        TempData["SuccessMessage"] = "Đăng ký thành công!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            _logger.LogInformation("User {UserId} logged out", userId);
        }

        ClearUserSession();
        Response.Cookies.Delete("RememberMe");

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (!IsUserLoggedIn())
        {
            return RedirectToAction("Login");
        }
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!IsUserLoggedIn())
        {
            return RedirectToAction("Login");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login");
        }

        var request = model.ToRequest();
        var (success, message) = await _authService.ChangePasswordAsync(userId.Value, request);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction("ChangePassword");
    }

    #region Session Helpers

    private bool IsUserLoggedIn()
    {
        return HttpContext.Session.GetString("UserId") != null;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }

    private void SetUserSession(UserInfo userInfo)
    {
        HttpContext.Session.SetString("UserId", userInfo.UserId.ToString());
        HttpContext.Session.SetString("Username", userInfo.Username);
        HttpContext.Session.SetString("Email", userInfo.Email);
        HttpContext.Session.SetString("FullName", userInfo.FullName ?? userInfo.Username);
        
        if (userInfo.Roles.Any())
        {
            HttpContext.Session.SetString("Roles", string.Join(",", userInfo.Roles));
        }
    }

    private void ClearUserSession()
    {
        HttpContext.Session.Clear();
    }

    #endregion
}
