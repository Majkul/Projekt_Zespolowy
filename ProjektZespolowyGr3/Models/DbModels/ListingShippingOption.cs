using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class ListingShippingOption
    {
        public int Id { get; set; }

        public int ListingId { get; set; }
        public Listing Listing { get; set; } = null!;

        /// <summary>Nazwa przewoźnika / metody dostawy (np. "InPost Paczkomat").</summary>
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Koszt dostawy. 0 = bezpłatna (odbiór osobisty).</summary>
        [Range(0, 9999.99)]
        public decimal Price { get; set; }
    }
}
