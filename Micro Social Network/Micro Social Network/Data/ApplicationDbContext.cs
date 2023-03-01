using Micro_Social_Network.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Micro_Social_Network.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // definire primary key compus
            modelBuilder.Entity<FriendRequest>()
            .HasKey(ab => new { ab.Id, ab.UserSendId, ab.UserReceiveId });
            // definire relatii cu modelele User si User
            modelBuilder.Entity<FriendRequest>()
            .HasOne(ab => ab.UserReceive)
            .WithMany(ab => ab.FriendRequestsReceived)
            .HasForeignKey(ab => ab.UserReceiveId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
            .HasOne(ab => ab.UserSend)
            .WithMany(ab => ab.FriendRequestsSent)
            .HasForeignKey(ab => ab.UserSendId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}