using HubNewsCollection.Domain.Response;
using Microsoft.EntityFrameworkCore;

namespace HubNewsCollection.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Articles> Articles => Set<Articles>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
           // mb.Entity<Articles>().ToTable("articles");
            mb.Entity<Articles>(e =>
            {
                e.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
                e.Property(x => x.Title).HasMaxLength(300).IsRequired();
                e.Property(x => x.Url).HasMaxLength(800).IsRequired();
                e.HasIndex(x => x.Url).IsUnique();  // evita duplicatas ao sync
                e.Property(x => x.Category).HasMaxLength(50);
            });


        }
    }
}
