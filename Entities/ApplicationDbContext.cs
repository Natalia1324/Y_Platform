using Microsoft.EntityFrameworkCore;

namespace Y_Platform.Entities
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Posts> Posts { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<PostVotes> PostVotes { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relacja PostVotes -> Posts
            modelBuilder.Entity<PostVotes>()
                .HasOne(pv => pv.Post)
                .WithMany(p => p.PostVotes)
                .OnDelete(DeleteBehavior.Restrict);  // Kaskadowe usuwanie głosów przy usunięciu posta

            // Relacja Users -> Posts
            modelBuilder.Entity<Users>()
                .HasMany(u => u.Posts)
                .WithOne(p => p.Users)
                .OnDelete(DeleteBehavior.Restrict);  // Kaskadowe usuwanie postów przy usunięciu użytkownika

            // Relacja PostVotes -> Users
            modelBuilder.Entity<PostVotes>()
                .HasOne(pv => pv.User)
                .WithMany(u => u.PostVotes)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
