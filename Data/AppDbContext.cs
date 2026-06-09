using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MessagingAPI.Models;

namespace MessagingAPI.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageGroup> MessageGroups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // Birebir mesajlaşmadaki ilişkilerde cascade silmeyi engelleme (hata almamak için)
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

                // Gruplara atılan mesajlardaki ilişkilerde cascade silmeyi engelleme
builder.Entity<GroupMessage>()
    .HasOne(gm => gm.Group)
    .WithMany(g => g.GroupMessages)
    .HasForeignKey(gm => gm.GroupId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<GroupMessage>()
    .HasOne(gm => gm.Sender)
    .WithMany()
    .HasForeignKey(gm => gm.SenderId)
    .OnDelete(DeleteBehavior.Restrict);

    // UserGroup ara tablosundaki ilişkilerde cascade silmeyi engelleme
builder.Entity<UserGroup>()
    .HasOne(ug => ug.Group)
    .WithMany(g => g.UserGroups)
    .HasForeignKey(ug => ug.GroupId)
    .OnDelete(DeleteBehavior.Restrict);

builder.Entity<UserGroup>()
    .HasOne(ug => ug.User)
    .WithMany()
    .HasForeignKey(ug => ug.UserId)
    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}