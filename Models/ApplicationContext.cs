using Microsoft.EntityFrameworkCore;

namespace Cua.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Admin)
                .WithMany(u => u.AdminRooms)
                .HasForeignKey(r => r.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

        }
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}