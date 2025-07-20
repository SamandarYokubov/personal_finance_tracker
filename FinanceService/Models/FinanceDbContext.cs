using Microsoft.EntityFrameworkCore;
using FinanceService.Models;

namespace FinanceService.Models
{
    public class FinanceDbContext : DbContext
    {
        public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }
        public DbSet<Transaction> Transactions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("finance");
            base.OnModelCreating(modelBuilder);
        }
    }
} 