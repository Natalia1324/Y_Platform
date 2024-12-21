using Microsoft.EntityFrameworkCore;

namespace Y_Platform.Entities
{
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Kontekst bazy danych
        /// </summary>
        public DbSet<Posts> Posts { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<PostVotes> PostVotes { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ///<summary>
            /// Wyznaczanie relacji między tabelami
            /// </summary>
            
            // Relacja PostVotes -> Posts
            modelBuilder.Entity<PostVotes>()
                .HasOne(pv => pv.Post)
                .WithMany(p => p.PostVotes)
                .OnDelete(DeleteBehavior.Cascade); 

            // Relacja Users -> Posts
            modelBuilder.Entity<Users>()
                .HasMany(u => u.Posts)
                .WithOne(p => p.Users)
                .OnDelete(DeleteBehavior.Cascade); 

            // Relacja PostVotes -> Users
            modelBuilder.Entity<PostVotes>()
                .HasOne(pv => pv.User)
                .WithMany(u => u.PostVotes)
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(modelBuilder);
        }
    }
}
