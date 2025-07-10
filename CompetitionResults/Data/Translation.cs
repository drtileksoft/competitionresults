using System.ComponentModel.DataAnnotations;

namespace CompetitionResults.Data
{
    public class Translation
    {
        public int Id { get; set; }

        [Required]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
