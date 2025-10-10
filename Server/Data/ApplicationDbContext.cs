using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Your existing DbSets
        public DbSet<Article> Articles { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<VATTable> VATTable { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LocalizationString> LocalizationStrings { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SalesInvoice> SalesInvoices { get; set; }
        public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }
        public DbSet<SalesCategory> SalesCategories { get; set; } = null!;
        public DbSet<BusinessUnit> BusinessUnits { get; set; }
        public DbSet<UserBusinessUnit> UserBusinessUnits { get; set; }
        public DbSet<InvoiceNumberFormat> InvoiceNumberFormats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Your existing Unit configuration
            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasKey(e => e.UnitId);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(50);
            });

            // Your existing Currency configuration
            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasKey(e => e.CurrencyId);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(5);
            });

            // Your existing VATTable configuration
            modelBuilder.Entity<VATTable>(entity =>
            {
                entity.HasKey(e => e.VATId);
                entity.Property(e => e.VATName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.VATRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Description).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Your existing Article configuration
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Barcode).HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").HasDefaultValue(0);
                entity.Property(e => e.StockQuantity).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.Category).HasMaxLength(100);

                // Relationships
                entity.HasOne(e => e.Unit)
                    .WithMany(u => u.Articles)
                    .HasForeignKey(e => e.UnitId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Currency)
                    .WithMany(c => c.Articles)
                    .HasForeignKey(e => e.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.VATTable)
                    .WithMany(v => v.Articles)
                    .HasForeignKey(e => e.VATId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Language configuration
            modelBuilder.Entity<Language>(entity =>
            {
                entity.HasKey(e => e.LanguageId);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(3);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // LocalizationString configuration
            modelBuilder.Entity<LocalizationString>(entity =>
            {
                entity.HasKey(e => e.StringId);
                entity.HasIndex(e => new { e.StringKey, e.LanguageId }).IsUnique();
                entity.Property(e => e.StringKey).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Text).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Language)
                    .WithMany(l => l.LocalizationStrings)
                    .HasForeignKey(e => e.LanguageId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // UserSetting configuration (Updated for Identity)
            modelBuilder.Entity<UserSetting>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasMaxLength(450);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Language)
                    .WithMany(l => l.UserSettings)
                    .HasForeignKey(e => e.SelectedLanguageId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserBusinessUnit>()
                .HasKey(ub => new { ub.UserId, ub.BusinessUnitId });

            modelBuilder.Entity<UserBusinessUnit>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBusinessUnits)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBusinessUnit>()
                .HasOne(ub => ub.BusinessUnit)
                .WithMany(b => b.UserBusinessUnits)
                .HasForeignKey(ub => ub.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceNumberFormat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Separator).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                // Add unique constraint per business unit and sales category
                entity.HasIndex(e => new { e.BusinessUnitId, e.SalesCategoryId })
                      .IsUnique();

                // Add relationships
                entity.HasOne(e => e.BusinessUnit)
                      .WithMany()
                      .HasForeignKey(e => e.BusinessUnitId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SalesCategory)
                      .WithMany()
                      .HasForeignKey(e => e.SalesCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // BusinessUnit configuration
            modelBuilder.Entity<BusinessUnit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}