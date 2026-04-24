using CaseBridge_Cases.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
namespace CaseBridge_Cases.Data
{
    public class CaseDbContext : DbContext
    {
        public CaseDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Case> Cases { get; set; }
        public DbSet<CaseHistory> CaseHistories { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tell EF Core to save the Enum as a readable String in the database
            modelBuilder.Entity<Case>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<CaseHistory>()
              .Property(h => h.PreviousStatus)
              .HasConversion<string>();

            modelBuilder.Entity<CaseHistory>()
                .Property(h => h.NewStatus)
                .HasConversion<string>();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationtoken = default)
        {
            var modifiedCases = ChangeTracker.Entries<Case>()
                .Where(x => x.State == EntityState.Modified);

            foreach (var entry in modifiedCases)
            {
                var statusProperty = entry.Property(c => c.Status);

                if (statusProperty.IsModified)
                {
                    var oldStatus = statusProperty.OriginalValue;
                    var newStatus = statusProperty.CurrentValue;


                    var history = new CaseHistory
                    {
                        CaseId = entry.Entity.Id,
                        Title = entry.Entity.Title,
                        Description = entry.Entity.Description,
                        Category = entry.Entity.Category,

                        PreviousStatus = oldStatus,
                        NewStatus = newStatus,
                        ChangedAt = DateTime.UtcNow,

                        // Grab the ID of whoever triggered this change
                        ModifiedByUserId = entry.Entity.LastModifiedByUserId,
                    };
                    CaseHistories.Add(history);
                }
            }
            return await base.SaveChangesAsync(cancellationtoken);
        }
    }
}
