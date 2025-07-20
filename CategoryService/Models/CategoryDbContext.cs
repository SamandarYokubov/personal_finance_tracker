using Microsoft.EntityFrameworkCore;

namespace CategoryService.Models
{
    public class CategoryDbContext : DbContext
    {
        public CategoryDbContext(DbContextOptions<CategoryDbContext> options) : base(options) { }
        public DbSet<Category> Categories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("category");
            base.OnModelCreating(modelBuilder);
        }
    }
} 