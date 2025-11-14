using Microsoft.EntityFrameworkCore;
using ST10445050_CLDV6212_POE_Part1.Models;

namespace ST10445050_CLDV6212_POE_Part1.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customer { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
