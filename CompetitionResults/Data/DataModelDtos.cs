namespace CompetitionResults.Data
{
    using System.ComponentModel.DataAnnotations;

    public class CompetitionDto
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

        public string? EmailTemplateFooter { get; set; }

		public string? EmailTemplateFooterCZ { get; set; }
	}

    public class CompetitionManagerDto
    {
        public int CompetitionId { get; set; }
        public string ManagerId { get; set; }
    }

    public class ThrowerDto
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

        // Foreign key for Competition
        public int CompetitionId { get; set; }

		public bool IsCampingOnSite { get; set; }

		public bool WantTShirt { get; set; }

        public string? TShirtSize { get; set; }

        public bool PaymentDone { get; set; }

        public double? Payment { get; set; }

        public bool DoNotSendRegistrationEmail { get; set; }

        public int StartingNumber { get; set; }
    }

    public class CategoryDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        // Foreign key for Competition
        public int CompetitionId { get; set; }
    }

    public class DisciplineDto
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

    }

    public class ResultsDto
    {
        // Composite key, configured in DbContext

        public int ThrowerId { get; set; }

        public int DisciplineId { get; set; }

        public int CompetitionId { get; set; }

        public double? Points { get; set; }

        public int? BullseyeCount { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string DomainName { get; set; }
    }

    public class UserRoleDto
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }
    }
}
