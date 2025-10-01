namespace CompetitionResults.Data
{
    using CompetitionResults.Backup;
    using CompetitionResults.Constants;
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

            // Adding indexes for optimization
            modelBuilder.Entity<CompetitionManager>()
                .HasIndex(cm => cm.ManagerId);

            modelBuilder.Entity<CompetitionManager>()
                .HasIndex(cm => new { cm.ManagerId, cm.CompetitionId });

            modelBuilder.Entity<Results>()
                .HasIndex(r => new { r.CompetitionId, r.DisciplineId });

            modelBuilder.Entity<Results>()
                .HasIndex(r => new { r.CompetitionId, r.ThrowerId });

            modelBuilder.Entity<Translation>()
                .HasIndex(t => t.LocalLanguage);

            modelBuilder.Entity<Translation>()
                .HasIndex(t => new { t.Key, t.LocalLanguage })
                .IsUnique();

            // Seeding the data
            modelBuilder.Entity<Competition>().HasData(
                new Competition { Id = 1, Name = "Your competition name", LocalLanguage="CZ", CompetitionPriceEUR = 90, CompetitionPriceLocal = 2200, EnableMissingResultPenalty = true }
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
                new Translation { Id = 1, Key = TranslationKeys.CampingOnSite, LocalLanguage = "CZ", Value = "Kempování na místě" },
                new Translation { Id = 2, Key = TranslationKeys.WantTShirt, LocalLanguage = "CZ", Value = "Chci tričko" },
                new Translation { Id = 3, Key = TranslationKeys.TShirtSize, LocalLanguage = "CZ", Value = "Velikost trička" },
                new Translation { Id = 4, Key = TranslationKeys.RegistrationForCompetition, LocalLanguage = "CZ", Value = "Registrace do závodu" },
                new Translation { Id = 5, Key = TranslationKeys.GeneralAnnouncement, LocalLanguage = "CZ", Value = "Obecná zpráva" },
                new Translation { Id = 6, Key = TranslationKeys.ImportantPaymentForCompetition, LocalLanguage = "CZ", Value = "Dulezite - Platba za registraci" },
                new Translation { Id = 7, Key = TranslationKeys.RegisteredToCompetition, LocalLanguage = "CZ", Value = "Byl/a jste úspěšně registrován/a na soutěž:" },
                new Translation { Id = 8, Key = TranslationKeys.Name, LocalLanguage = "CZ", Value = "Jméno" },
                new Translation { Id = 9, Key = TranslationKeys.Surname, LocalLanguage = "CZ", Value = "Příjmení" },
                new Translation { Id = 10, Key = TranslationKeys.Nickname, LocalLanguage = "CZ", Value = "Přezdívka" },
                new Translation { Id = 11, Key = TranslationKeys.Nationality, LocalLanguage = "CZ", Value = "Národnost" },
                new Translation { Id = 12, Key = TranslationKeys.ClubName, LocalLanguage = "CZ", Value = "Jméno klubu" },
                new Translation { Id = 13, Key = TranslationKeys.Email, LocalLanguage = "CZ", Value = "Email" },
                new Translation { Id = 14, Key = TranslationKeys.Note, LocalLanguage = "CZ", Value = "Poznámka" },
                new Translation { Id = 15, Key = TranslationKeys.Category, LocalLanguage = "CZ", Value = "Kategorie" },
                new Translation { Id = 16, Key = TranslationKeys.Hello, LocalLanguage = "CZ", Value = "Dobrý den," },
                new Translation { Id = 17, Key = TranslationKeys.UnpaidEmailIntro, LocalLanguage = "CZ", Value = "Tento email je automaticky generován, protože jste se zaregistrovali na soutěž a ještě jste nezaplatili." },
                new Translation { Id = 18, Key = TranslationKeys.ParticipantLimit, LocalLanguage = "CZ", Value = "Limit pro počet účastníků byl nastaven na {0}. Registrace je finální až po zaplacení." },
                new Translation { Id = 19, Key = TranslationKeys.PaymentStats, LocalLanguage = "CZ", Value = "Aktuálně má zaplaceno {0} z {1} účastníků." },
                new Translation { Id = 20, Key = TranslationKeys.PayAsap, LocalLanguage = "CZ", Value = "Prosím, zaplaťte co nejdříve, jinak Vás předběhne někdo jiný a nebudete se moci zúčastnit soutěže." },
                new Translation { Id = 21, Key = TranslationKeys.ThankYou, LocalLanguage = "CZ", Value = "Děkujeme." },
                new Translation { Id = 22, Key = TranslationKeys.Team, LocalLanguage = "CZ", Value = "Tým {0}" }
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
