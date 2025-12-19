using System.Collections.Generic;
using MVC_project.Models;

namespace MVC_project.ViewModels
{
    public class UserDetailsViewModel
    {
        public User User { get; set; } = new User();
        public List<UserTrip> Bookings { get; set; } = new List<UserTrip>();
    }
}
