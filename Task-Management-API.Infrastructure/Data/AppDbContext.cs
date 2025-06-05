using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Task_Management_API.Domain.Models;

namespace Task_Management_API.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<AppTask> Tasks { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

    }
}
