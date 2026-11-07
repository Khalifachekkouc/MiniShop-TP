using Microsoft.EntityFrameworkCore;
using MiniShop.Models;

namespace MiniShop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
                
        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Produit> Produits { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }
        public DbSet<ProductStorage> ProductStorages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Names are unique whatever the case: the database collation is case-insensitive.
            modelBuilder.Entity<Category>(c =>
            {
                c.Property(x => x.Name).IsRequired().HasMaxLength(Category.NameStorage);
                c.Property(x => x.Slug).IsRequired().HasMaxLength(CategorySlug.MaxLength + 4);
                c.HasIndex(x => x.Name).IsUnique();
                c.HasIndex(x => x.Slug).IsUnique();
            });
            // A category with products cannot be deleted: move or delete its products first.
            modelBuilder.Entity<Produit>().HasOne(p => p.Category).WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Produit>().HasMany(p => p.Colors).WithOne(c => c.Produit!)
                .HasForeignKey(c => c.ProduitId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Produit>().HasMany(p => p.Storages).WithOne(s => s.Produit!)
                .HasForeignKey(s => s.ProduitId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
