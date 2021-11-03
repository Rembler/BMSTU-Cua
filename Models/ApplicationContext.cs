using Microsoft.EntityFrameworkCore;

namespace Cua.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Queue> Queues { get; set; }
        public DbSet<QueueUser> QueueUsers { get; set; }
        public DbSet<RoomUser> RoomUsers { get; set; }
        public DbSet<Request> Requests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Admin)
                .WithMany(u => u.AdminRooms)
                .HasForeignKey(r => r.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Queue>()
                .HasOne(q => q.Creator)
                .WithMany(u => u.CreatedQueues)
                .HasForeignKey(q => q.CreatorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<QueueUser>()
                .HasKey(qu => new { qu.QueueId, qu.UserId });

            modelBuilder.Entity<RoomUser>()
                .HasKey(ru => new { ru.RoomId, ru.UserId });

            modelBuilder.Entity<Request>()
                .HasKey(r => new { r.RoomId, r.UserId });
        }
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
            // Database.EnsureDeleted();
            Database.EnsureCreated();
        }
    }
}