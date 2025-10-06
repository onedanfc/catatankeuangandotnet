using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatatanKeuanganDotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace CatatanKeuanganDotnet.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Id)
                    .HasMaxLength(10)
                    .IsRequired()
                    .ValueGeneratedNever();
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.UserId)
                    .HasMaxLength(10)
                    .IsRequired();
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.UserId)
                    .HasMaxLength(10)
                    .IsRequired();
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.Property(t => t.UserId)
                    .HasMaxLength(10)
                    .IsRequired();
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            EnsureUserIds();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            EnsureUserIds();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void EnsureUserIds()
        {
            foreach (var entry in ChangeTracker.Entries<User>().Where(e => e.State == EntityState.Added))
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.Id))
                {
                    entry.Entity.Id = User.GenerateId();
                }
            }
        }
    }
}
