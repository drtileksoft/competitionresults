namespace CompetitionResults.Data
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;

    public class Competition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public bool CampingOnSiteAvailable { get; set; }

        public bool TShirtAvailable { get; set; }

        public string? TShirtLink { get; set; }

        public int? MaxCompetitorCount { get; set; }

        public string? EmailTemplateFooter { get; set; }

		public string? EmailTemplateFooterCZ { get; set; }

		// Navigation properties for related entities
		public ICollection<Thrower> Throwers { get; set; }
        public ICollection<Category> Categories { get; set; }
        public ICollection<Discipline> Disciplines { get; set; }
        public ICollection<Results> Results { get; set; }
        public ICollection<CompetitionManager> CompetitionManagers { get; set; }
    }

    public class CompetitionManager
    {
        public int CompetitionId { get; set; }
        public string ManagerId { get; set; }

        public Competition Competition { get; set; }
        public ApplicationUser Manager { get; set; }
    }

    public class Thrower
        {
                [Key]
                public int Id { get; set; }

		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		[Required]
		[StringLength(50)]
		public string Surname { get; set; }

        [StringLength(50)]
        public string? Nickname { get; set; }

        [Required]
		[StringLength(50)]
		public string Nationality { get; set; }

		[StringLength(100)]
		public string? ClubName { get; set; }

		[EmailAddress]
		public string? Email { get; set; }

		public string? Note { get; set; }

		// Foreign key for Category
		public int CategoryId { get; set; }

		// Navigation property for Category
		[ForeignKey("CategoryId")]
		public Category Category { get; set; }

        // Foreign key for Competition
        public int CompetitionId { get; set; }

        // Navigation property for Competition
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }

        public bool IsCampingOnSite { get; set; }

		public bool WantTShirt { get; set; }

        public string? TShirtSize { get; set; }

        public bool PaymentDone { get; set; }

        public double? Payment { get; set; }

        public bool DoNotSendRegistrationEmail { get; set; }

        public int StartingNumber { get; set; }
    }

	public class Category
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		// Navigation property for Throwers
		public ICollection<Thrower> Throwers { get; set; }

        // Foreign key for Competition
        public int CompetitionId { get; set; }

        // Navigation property for Competition
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }
    }

	public class Discipline
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		public bool HasPositionsInsteadPoints { get; set; }

        public bool HasDecimalPoints { get; set; }

        public bool IsDividedToCategories { get; set; }

        // Foreign key for Competition
        public int CompetitionId { get; set; }

        // Navigation property for Competition
        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }
    }

	public class Results
	{
		// Composite key, configured in DbContext

		public int ThrowerId { get; set; }

		public int DisciplineId { get; set; }

        public int CompetitionId { get; set; }

        // Navigation properties
        [ForeignKey("ThrowerId")]
		public Thrower Thrower { get; set; }

		[ForeignKey("DisciplineId")]
		public Discipline Discipline { get; set; }

        [ForeignKey("CompetitionId")]
        public Competition Competition { get; set; }

        public double? Points { get; set; }

        public int? BullseyeCount { get; set; }

    }

}
