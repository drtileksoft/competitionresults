using Microsoft.AspNetCore.Identity;

namespace CompetitionResults.Data
{
	public class ApplicationUser : IdentityUser
	{
		public string DomainName { get; set; }
        public ICollection<CompetitionManager> CompetitionManagers { get; set; }
    }
}
