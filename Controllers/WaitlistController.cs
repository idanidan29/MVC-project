using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVC_project.Services;

namespace MVC_project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class WaitlistController : Controller
    {
        private readonly WaitlistService _waitlistService;

        public WaitlistController(WaitlistService waitlistService)
        {
            _waitlistService = waitlistService;
        }

        // POST: /Waitlist/ProcessNotifications - Manually trigger notification processing
        [HttpPost]
        public async Task<IActionResult> ProcessNotifications()
        {
            await _waitlistService.ProcessPendingNotificationsAsync();
            return Json(new { success = true, message = "Waitlist notifications processed successfully." });
        }
    }
}
