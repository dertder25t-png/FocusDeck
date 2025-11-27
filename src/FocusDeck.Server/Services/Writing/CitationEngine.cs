using System.Text;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Server.Services.Writing
{
    public interface ICitationEngine
    {
        string GenerateCitation(AcademicSource source, string style);
        string GenerateBibliography(List<AcademicSource> sources, string style);
    }

    public class CitationEngine : ICitationEngine
    {
        public string GenerateCitation(AcademicSource source, string style)
        {
            return style.ToUpper() switch
            {
                "APA" => GenerateApa(source),
                "MLA" => GenerateMla(source),
                "CHICAGO" => GenerateChicago(source),
                _ => GenerateApa(source)
            };
        }

        public string GenerateBibliography(List<AcademicSource> sources, string style)
        {
            var sb = new StringBuilder();
            var ordered = sources.OrderBy(s => s.Author).ToList(); // Alphabetical order

            foreach (var source in ordered)
            {
                sb.AppendLine(GenerateCitation(source, style));
            }

            return sb.ToString();
        }

        private string GenerateApa(AcademicSource s)
        {
            // APA: Author, A. A. (Year). Title of work. Publisher. URL
            var sb = new StringBuilder();

            // Author (Simplified last name first logic)
            if (!string.IsNullOrEmpty(s.Author))
            {
                sb.Append($"{s.Author} ");
            }

            // Year
            sb.Append($"({s.Year}). ");

            // Title (Italicized in markdown)
            if (!string.IsNullOrEmpty(s.Title))
            {
                sb.Append($"*{s.Title}*. ");
            }

            // Publisher
            if (!string.IsNullOrEmpty(s.Publisher))
            {
                sb.Append($"{s.Publisher}. ");
            }

            // URL/DOI
            if (!string.IsNullOrEmpty(s.Doi))
            {
                sb.Append($"https://doi.org/{s.Doi}");
            }
            else if (!string.IsNullOrEmpty(s.Url))
            {
                sb.Append(s.Url);
            }

            return sb.ToString().Trim();
        }

        private string GenerateMla(AcademicSource s)
        {
            // MLA: Author. Title. Publisher, Year.
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(s.Author))
            {
                sb.Append($"{s.Author}. ");
            }

            if (!string.IsNullOrEmpty(s.Title))
            {
                sb.Append($"*{s.Title}*. ");
            }

            if (!string.IsNullOrEmpty(s.Publisher))
            {
                sb.Append($"{s.Publisher}, ");
            }

            sb.Append($"{s.Year}.");

            return sb.ToString().Trim();
        }

        private string GenerateChicago(AcademicSource s)
        {
            // Chicago: Author. Title. Place: Publisher, Year. (Simplified)
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(s.Author))
            {
                sb.Append($"{s.Author}. ");
            }

            if (!string.IsNullOrEmpty(s.Title))
            {
                sb.Append($"*{s.Title}*. ");
            }

            if (!string.IsNullOrEmpty(s.Publisher))
            {
                sb.Append($"{s.Publisher}, ");
            }

            sb.Append($"{s.Year}.");

            return sb.ToString().Trim();
        }
    }
}
