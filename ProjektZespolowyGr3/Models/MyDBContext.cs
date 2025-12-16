using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models
{
    public class MyDBContext : DbContext
    {
        public MyDBContext(DbContextOptions<MyDBContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<UserAuth> UserAuths { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Upload> Uploads { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ListingPhoto> ListingPhotos { get; set; }
        public DbSet<ListingTag> ListingTags { get; set; }
        public DbSet<ReviewPhoto> ReviewPhotos { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ListingTag>()
                .HasKey(lt => new { lt.ListingId, lt.TagId });

            modelBuilder.Entity<ListingTag>()
                .HasOne(lt => lt.Listing)
                .WithMany(l => l.Tags)
                .HasForeignKey(lt => lt.ListingId);

            modelBuilder.Entity<ListingTag>()
                .HasOne(lt => lt.Tag)
                .WithMany(t => t.ListingTags)
                .HasForeignKey(lt => lt.TagId);
        }
    }
}
