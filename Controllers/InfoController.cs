using Microsoft.AspNetCore.Mvc;

namespace MVC_project.Controllers
{
    public class InfoController : Controller
    {
        [HttpGet("")]
        [HttpGet("Info")]
        [HttpGet("Info/Index")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
