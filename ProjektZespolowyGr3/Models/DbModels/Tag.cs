using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Tag
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; }

        // potem moze jakies kategorie czy cos

        public ICollection<ListingTag> ListingTags { get; set; } = new List<ListingTag>();
    }
}
