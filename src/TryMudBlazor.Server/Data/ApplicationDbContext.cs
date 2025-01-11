namespace TryMudBlazor.Server.Data
{
    using Microsoft.EntityFrameworkCore;
    using TryMudBlazor.Server.Data.Models;

    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<SnippetBlob> SnippetBlobs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SnippetBlob>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Id)
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasMaxLength(16) 
                    .IsRequired();

                entity.Property(e => e.Content)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
