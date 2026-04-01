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
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<TradeProposal> TradeProposals { get; set; }
        public DbSet<TradeProposalItem> TradeProposalItems { get; set; }
        public DbSet<TradeProposalHistoryEntry> TradeProposalHistoryEntries { get; set; }
        public DbSet<ListingExchangeAcceptedTag> ListingExchangeAcceptedTags { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ListingShippingOption> ListingShippingOptions { get; set; }
        public DbSet<TradeOrder> TradeOrders { get; set; }

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

            modelBuilder.Entity<ListingExchangeAcceptedTag>()
                .HasKey(x => new { x.ListingId, x.TagId });

            modelBuilder.Entity<ListingExchangeAcceptedTag>()
                .HasOne(x => x.Listing)
                .WithMany(l => l.ExchangeAcceptedTags)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ListingExchangeAcceptedTag>()
                .HasOne(x => x.Tag)
                .WithMany(t => t.ListingExchangeAcceptedTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TradeProposal>()
                .HasOne(t => t.Initiator)
                .WithMany(u => u.TradeProposalsAsInitiator)
                .HasForeignKey(t => t.InitiatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeProposal>()
                .HasOne(t => t.Receiver)
                .WithMany(u => u.TradeProposalsAsReceiver)
                .HasForeignKey(t => t.ReceiverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeProposal>()
                .HasOne(t => t.SubjectListing)
                .WithMany(l => l.TradeProposalsAsSubject)
                .HasForeignKey(t => t.SubjectListingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeProposal>()
                .HasOne(t => t.ParentTradeProposal)
                .WithMany(t => t.CounterOffers)
                .HasForeignKey(t => t.ParentTradeProposalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeProposal>()
                .HasOne(t => t.RootTradeProposal)
                .WithMany()
                .HasForeignKey(t => t.RootTradeProposalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeProposalItem>()
                .HasOne(i => i.TradeProposal)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.TradeProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TradeProposalItem>()
                .HasOne(i => i.Listing)
                .WithMany()
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeProposalHistoryEntry>()
                .HasOne(h => h.TradeProposal)
                .WithMany(p => p.History)
                .HasForeignKey(h => h.TradeProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TradeProposalHistoryEntry>()
                .HasOne(h => h.User)
                .WithMany(u => u.TradeProposalHistoryEntries)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacje wiadomości <-> użytkownicy
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.TradeProposal)
                .WithMany(p => p.Messages)
                .HasForeignKey(m => m.TradeProposalId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Listing)
                .WithMany()
                .HasForeignKey(o => o.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ListingShippingOption>()
                .HasOne(s => s.Listing)
                .WithMany(l => l.ShippingOptions)
                .HasForeignKey(s => s.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TradeOrder>()
                .HasOne(t => t.TradeProposal)
                .WithMany()
                .HasForeignKey(t => t.TradeProposalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Message)
                .WithMany()
                .HasForeignKey(n => n.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Order)
                .WithMany()
                .HasForeignKey(n => n.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.TradeProposal)
                .WithMany()
                .HasForeignKey(n => n.TradeProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
