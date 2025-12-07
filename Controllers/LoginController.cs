using Microsoft.AspNetCore.Mvc;
using MVC_project.Data;
using MVC_project.Services;
using MVC_project.ViewModels;

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
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _repo.GetByEmail(model.Email);
            if (user == null || !_passwordService.VerifyPassword(model.Password, user.passwordHash))
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // Redirect based on user role
            if (user.admin)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
