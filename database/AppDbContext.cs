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

            // Mapeia a entidade Articles (supondo propriedades lower-case no seu modelo)
            mb.Entity<Articles>(e =>
            {
                e.ToTable("articles");

                e.HasKey(x => x.id);

                e.Property(x => x.id)
                    .HasColumnName("id")
                    .HasColumnType("uniqueidentifier");

                e.Property(x => x.title)
                    .HasColumnName("title")
                    .HasMaxLength(300)
                    .IsRequired();

                e.Property(x => x.author)
                    .HasColumnName("author")
                    .HasMaxLength(150);

                e.Property(x => x.source)
                    .HasColumnName("source")
                    .HasMaxLength(150);

                e.Property(x => x.category)
                    .HasColumnName("category")
                    .HasMaxLength(50)
                    .HasDefaultValue("business");

                e.Property(x => x.description)
                    .HasColumnName("description")
                    .HasColumnType("nvarchar(max)");

                e.Property(x => x.url)
                    .HasMaxLength(2048)
                    .IsRequired();

                e.Property(x => x.image)
                    .HasColumnName("image")
                    .HasMaxLength(500);

                e.Property(x => x.published_at)
                    .HasColumnName("published_at")
                    .HasColumnType("datetime2");


                e.HasIndex(x => x.url)
                    .IsUnique()
                    .HasDatabaseName("IX_articles_url");
            });
        }
    }
}
