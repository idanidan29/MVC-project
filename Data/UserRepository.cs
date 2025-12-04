using MVC_project.Models;
using System;

namespace MVC_project.Data
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public bool EmailExists(string email)
        {
            return _context.Users.Any(u => u.email == email);
        }

        public void Add(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public User GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.email == email);
        }
    }
}
