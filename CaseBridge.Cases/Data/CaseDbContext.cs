using Microsoft.EntityFrameworkCore;
using CaseBridge_Cases.Models;
namespace CaseBridge_Cases.Data
{
    public class CaseDbContext : DbContext
    {
        public CaseDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Case> Cases { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tell EF Core to save the Enum as a readable String in the database
            modelBuilder.Entity<Case>()
                .Property(c => c.Status)
                .HasConversion<string>();
        }
    }
}
