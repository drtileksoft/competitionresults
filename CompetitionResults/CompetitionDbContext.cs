namespace CompetitionResults.Data
{
    using CompetitionResults.Backup;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
	using Microsoft.EntityFrameworkCore;

	public class CompetitionDbContext : IdentityDbContext<ApplicationUser>
	{
        public DbSet<CompetitionManager> CompetitionManagers { get; set; }
        public DbSet<Competition> Competitions { get; set; }
        public DbSet<Thrower> Throwers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Discipline> Disciplines { get; set; }
        public DbSet<Results> Results { get; set; }
        public DbSet<Translation> Translations { get; set; }

        public CompetitionDbContext(DbContextOptions<CompetitionDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuring the composite key for Results
            modelBuilder.Entity<Results>()
                .HasKey(r => new { r.ThrowerId, r.DisciplineId, r.CompetitionId });


            // Configure one-to-many relationship between Category and Thrower
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Throwers)
                .WithOne(t => t.Category)
                .HasForeignKey(t => t.CategoryId);

            // Configure one-to-many relationship between Competition and other entities
            modelBuilder.Entity<Competition>()
                .HasMany(c => c.Throwers)
                .WithOne(t => t.Competition)
                .HasForeignKey(t => t.CompetitionId);

            modelBuilder.Entity<Competition>()
                .HasMany(c => c.Categories)
                .WithOne(cat => cat.Competition)
                .HasForeignKey(cat => cat.CompetitionId);

            modelBuilder.Entity<Competition>()
                .HasMany(c => c.Disciplines)
                .WithOne(d => d.Competition)
                .HasForeignKey(d => d.CompetitionId);

            modelBuilder.Entity<Competition>()
                .HasMany(c => c.Results)
                .WithOne(r => r.Competition)
                .HasForeignKey(r => r.CompetitionId);

            modelBuilder.Entity<CompetitionManager>()
            .HasKey(cm => new { cm.CompetitionId, cm.ManagerId });

            modelBuilder.Entity<CompetitionManager>()
                .HasOne(cm => cm.Competition)
                .WithMany(c => c.CompetitionManagers)
                .HasForeignKey(cm => cm.CompetitionId);

            modelBuilder.Entity<CompetitionManager>()
                .HasOne(cm => cm.Manager)
                .WithMany(m => m.CompetitionManagers)
                .HasForeignKey(cm => cm.ManagerId);

            // Optional: Further configurations can be added here

            // Seeding the data
            modelBuilder.Entity<Competition>().HasData(
                new Competition { Id = 1, Name = "Your competition name", LocalLanguage="CZ", CompetitionPriceEUR = 90, CompetitionPriceLocal = 2200 }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Men", CompetitionId = 1 },
                new Category { Id = 2, Name = "Women", CompetitionId = 1 }
            );

            modelBuilder.Entity<Discipline>().HasData(
                new Discipline { Id = 1, Name = "Walkback Nospin", CompetitionId = 1, IsDividedToCategories = true },
                new Discipline { Id = 2, Name = "Walkback Knife", CompetitionId = 1, IsDividedToCategories = true },
                new Discipline { Id = 3, Name = "Walkback Axe", CompetitionId = 1, IsDividedToCategories = true },
                new Discipline { Id = 4, Name = "Long Distance Nospin", HasDecimalPoints = true, CompetitionId = 1, IsDividedToCategories = true },
                new Discipline { Id = 5, Name = "Long Distance Knife", HasDecimalPoints = true, CompetitionId = 1, IsDividedToCategories = true },
                new Discipline { Id = 6, Name = "Long Distance Axe", HasDecimalPoints = true, CompetitionId = 1, IsDividedToCategories = true },
                new Discipline { Id = 7, Name = "Silhouette Nospin", CompetitionId = 1 },
                new Discipline { Id = 8, Name = "Silhouette Knife", CompetitionId = 1 },
                new Discipline { Id = 9, Name = "Silhouette Axe", CompetitionId = 1 },               
                new Discipline { Id = 11, Name = "Coutanque", HasPositionsInsteadPoints = true, CompetitionId = 1 },
                new Discipline { Id = 12, Name = "Duel", HasPositionsInsteadPoints = true, CompetitionId = 1 }

            );


            modelBuilder.Entity<Thrower>().HasData(
                new Thrower { Id = 1, Name = "Zuzana", Surname = "Koreňová", Nickname = "Suzanne KO", Nationality = "CZ", CompetitionId = 1, CategoryId = 2, StartingNumber = 1 }
            );

            modelBuilder.Entity<Translation>().HasData(
                new Translation { Id = 1, Key = "Camping on site", Value = "Kempování na místě" },
                new Translation { Id = 2, Key = "Want T-Shirt", Value = "Chci tričko" },
                new Translation { Id = 3, Key = "T-Shirt Size", Value = "Velikost trička" },
                new Translation { Id = 4, Key = "Registration for competition", Value = "Registrace do závodu" },
                new Translation { Id = 5, Key = "General Announcement", Value = "Obecná zpráva" },
                new Translation { Id = 6, Key = "Important - Payment for competition", Value = "Dulezite - Platba za registraci" },
                new Translation { Id = 7, Key = "You have been successfully registered to competition:", Value = "Byl/a jste úspěšně registrován/a na soutěž:" },
                new Translation { Id = 8, Key = "Name", Value = "Jméno" },
                new Translation { Id = 9, Key = "Surname", Value = "Příjmení" },
                new Translation { Id = 10, Key = "Nickname", Value = "Přezdívka" },
                new Translation { Id = 11, Key = "Nationality", Value = "Národnost" },
                new Translation { Id = 12, Key = "Club name", Value = "Jméno klubu" },
                new Translation { Id = 13, Key = "Email", Value = "Email" },
                new Translation { Id = 14, Key = "Note", Value = "Poznámka" },
                new Translation { Id = 15, Key = "Category", Value = "Kategorie" }
            );
        }

        public override int SaveChanges()
        {
            BackupTools.BackupDatabaseAsync().GetAwaiter().GetResult();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await BackupTools.BackupDatabaseAsync();
            return await base.SaveChangesAsync(cancellationToken);
        }

    }

}
