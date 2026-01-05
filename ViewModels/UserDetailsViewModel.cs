using System.Collections.Generic;
using MVC_project.Models;

namespace MVC_project.ViewModels
{
    public class UserDetailsViewModel
    {
        public User User { get; set; } = new User();
        public List<Booking> ActiveBookings { get; set; } = new List<Booking>();
        public List<Waitlist> ActiveWaitlists { get; set; } = new List<Waitlist>();
        public List<Booking> AllBookings { get; set; } = new List<Booking>();
        public int ActiveBookingTripCount { get; set; }
    }
}
