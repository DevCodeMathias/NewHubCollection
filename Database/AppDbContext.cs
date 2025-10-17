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

            mb.Entity<Articles>(e =>
            {
                e.ToTable("articles");                 
                e.HasKey(x => x.id);

                e.Property(x => x.id)
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()") 
                    .ValueGeneratedOnAdd();

                e.Property(x => x.title).IsRequired();
                e.Property(x => x.description).HasColumnType("text");
                e.Property(x => x.url).HasColumnType("text").IsRequired();
                e.HasIndex(x => x.url).IsUnique();
                e.Property(x => x.category).HasMaxLength(50);
            });
        }

    }
}
