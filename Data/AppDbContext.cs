using Microsoft.EntityFrameworkCore;
using MVC_project.Models;

namespace MVC_project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map the User entity to the dbo.Users table
            modelBuilder.Entity<User>()
                .ToTable("Users", "dbo");

            // Set 'email' as the primary key of the User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.email);

            base.OnModelCreating(modelBuilder);
        }
    }
}
