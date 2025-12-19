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

        public IEnumerable<User> GetAll()
        {
            return _context.Users
                .OrderBy(u => u.first_name)
                .ThenBy(u => u.last_name)
                .ToList();
        }

        public IEnumerable<User> Search(string? name, string? email)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailLower = email.ToLower();
                query = query.Where(u => u.email.ToLower().Contains(emailLower));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var nameLower = name.ToLower();
                query = query.Where(u =>
                    (u.first_name != null && u.first_name.ToLower().Contains(nameLower)) ||
                    (u.last_name != null && u.last_name.ToLower().Contains(nameLower)));
            }

            return query
                .OrderBy(u => u.first_name)
                .ThenBy(u => u.last_name)
                .ToList();
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void Delete(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.email == email);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }
    }
}
