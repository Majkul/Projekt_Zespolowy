using System.Text;
using System.Text.RegularExpressions;

namespace ProjektZespolowyGr3.Helpers
{
    public static class SlugHelper
    {
        private const int MaxSlugLength = 60;

        private static readonly Regex NonAlphanumericRegex = new("[^a-z0-9]+", RegexOptions.Compiled);
        private static readonly Regex MultipleHyphensRegex = new("-{2,}", RegexOptions.Compiled);

        public static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var normalized = ReplacePolishCharacters(input.ToLowerInvariant());
            var slug = NonAlphanumericRegex.Replace(normalized, "-");
            slug = MultipleHyphensRegex.Replace(slug, "-").Trim('-');

            if (slug.Length > MaxSlugLength)
            {
                slug = slug[..MaxSlugLength].Trim('-');
            }

            return slug;
        }

        private static string ReplacePolishCharacters(string input)
        {
            var builder = new StringBuilder(input.Length);

            foreach (var character in input)
            {
                builder.Append(character switch
                {
                    'ą' => 'a',
                    'ć' => 'c',
                    'ę' => 'e',
                    'ł' => 'l',
                    'ń' => 'n',
                    'ó' => 'o',
                    'ś' => 's',
                    'ż' => 'z',
                    'ź' => 'z',
                    _ => character
                });
            }

            return builder.ToString();
        }
    }
}
