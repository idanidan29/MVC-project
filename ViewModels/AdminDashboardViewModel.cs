using System.Collections.Generic;
using MVC_project.Models;

namespace MVC_project.ViewModels
{
    public class AdminUserItem
    {
        public User User { get; set; } = new User();
        public int BookingsCount { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public string? SearchName { get; set; }
        public string? SearchEmail { get; set; }
        public List<AdminUserItem> Users { get; set; } = new List<AdminUserItem>();
    }
}
