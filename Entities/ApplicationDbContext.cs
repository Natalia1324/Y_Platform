using Microsoft.EntityFrameworkCore;

namespace Y_Platform.Entities
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Post> Posts { get; set; }
        public DbSet<Users> Users { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Dodatkowa konfiguracja encji, jeśli potrzebna
            modelBuilder.Entity<Users>()
                .HasMany(u => u.Posts)
                .WithOne(p => p.Users);

            base.OnModelCreating(modelBuilder);
        }
    }
}
