using Microsoft.AspNetCore.Mvc;
using MVC_project.Models;
using MVC_project.ViewModels;
using MVC_project.Data;
using MVC_project.Services;

namespace MVC_project.Controllers
{
    public class UserController : Controller
    {
        private readonly UserRepository _repo;
        private readonly PasswordService _passwordService;

        public UserController(UserRepository repo, PasswordService passwordService)
        {
            _repo = repo;
            _passwordService = passwordService;
        }

        // GET: /User/Register
        public IActionResult Register()
        {
            // MVC will automatically look for Views/User/Register.cshtml
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Pass the model back to the same view in case of errors
                return View(model);
            }

            if (_repo.EmailExists(model.Email))
            {
                ModelState.AddModelError("Email", "This email address is already registered");
                return View(model);
            }

            var user = new User
            {
                first_name = model.FirstName,
                last_name = model.LastName,
                email = model.Email,
                passwordHash = _passwordService.HashPassword(model.Password),
                admin = false
            };

            _repo.Add(user);

            // Redirect to Login page after successful registration
            return RedirectToAction("Login", "Login"); // Specify controller if Login is in another controller
        }
    }
}
