using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ProjektZespolowyGr3.Models
{
    public class OfferViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Tytuł jest wymagany.")]
        [StringLength(80, MinimumLength = 10, ErrorMessage = "Tytuł musi mieć od 10 do 80 znaków.")]
        public string Title { get; set; }

        [StringLength(2000, ErrorMessage = "Opis może mieć maksymalnie 2000 znaków.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Typ oferty jest wymagany.")]
        [RegularExpression("^(oddaję|sprzedam)$", ErrorMessage = "Typ musi być 'oddaję' lub 'sprzedam'.")]
        public string Type { get; set; }  // "oddaję" or "sprzedam"

        [Range(0.01, double.MaxValue, ErrorMessage = "Cena musi być większa od 0.")]
        public decimal? Price { get; set; }  // optional

        [MaxLength(10, ErrorMessage = "Możesz podać maksymalnie 10 tagów.")]
        public List<string> Tags { get; set; }

        [Required(ErrorMessage = "Musisz dodać co najmniej jedno zdjęcie.")]
        [MinLength(1, ErrorMessage = "Musisz dodać co najmniej jedno zdjęcie.")]
        [MaxLength(5, ErrorMessage = "Możesz dodać maksymalnie 5 zdjęć.")]
        public List<IFormFile> Photos { get; set; }

        // Custom validation rules
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Rule: Cena > 0 tylko przy "sprzedam"
            if (Type == "oddaję" && Price.HasValue && Price.Value > 0)
            {
                yield return new ValidationResult(
                    "Cena nie może być ustawiona dla oferty typu 'oddaję'.",
                    new[] { nameof(Price) });
            }

            if (Type == "sprzedam" && (!Price.HasValue || Price.Value <= 0))
            {
                yield return new ValidationResult(
                    "Cena musi być większa od 0 dla oferty typu 'sprzedam'.",
                    new[] { nameof(Price) });
            }

            // Rule: zdjęcia max 2 MB i poprawny MIME
            if (Photos != null)
            {
                foreach (var photo in Photos)
                {
                    if (photo.Length > 2 * 1024 * 1024)
                    {
                        yield return new ValidationResult(
                            "Każde zdjęcie musi być mniejsze niż 2 MB.",
                            new[] { nameof(Photos) });
                    }

                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(photo.ContentType))
                    {
                        yield return new ValidationResult(
                            "Dozwolone są tylko zdjęcia JPEG, PNG lub GIF.",
                            new[] { nameof(Photos) });
                    }
                }
            }
        }
    }
}

