using Microsoft.EntityFrameworkCore;

namespace ProjektZespolowyGr3.Models
{
    public class MyDBContext : DbContext
    {
        public MyDBContext(DbContextOptions<MyDBContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Listing> Listings { get; set; }
    }
}
