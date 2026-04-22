using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.ViewModels
{
    public class ShippingOptionInput
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 9999.99)]
        public decimal Price { get; set; } = 0;
    }
}
