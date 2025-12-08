using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.Services;
using MVC_project.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MVC_project.Controllers
{
    [Route("Login")]     public class LoginController : Controller
    {
        private readonly UserRepository _repo;
        private readonly PasswordService _passwordService;

        public LoginController(UserRepository repo, PasswordService passwordService)
        {
            _repo = repo;
            _passwordService = passwordService;
        }

        // GET /Login
        [HttpGet("")]
        public IActionResult Login()
        {
            return View(); 
        }

        // POST /Login
        [HttpPost("")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _repo.GetByEmail(model.Email);
            if (user == null || !_passwordService.VerifyPassword(model.Password, user.passwordHash))
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // Create user claims and sign in with cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.email ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.email ?? ""),
                new Claim(ClaimTypes.Role, user.admin ? "Admin" : "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirect based on user role
            if (user.admin)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // GET /Login/Logout
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Login");
        }
    }
}
