using System.Text;
using System.Text.RegularExpressions;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Server.Services.Writing
{
    public interface ICitationEngine
    {
        string GenerateInTextCitation(AcademicSource source, string style);
        string GenerateCitation(AcademicSource source, string style);
        string GenerateBibliography(List<AcademicSource> sources, string style);
        string UpdateBibliography(Note note);
    }

    public class CitationEngine : ICitationEngine
    {
        public string GenerateInTextCitation(AcademicSource source, string style)
        {
            return style.ToUpper() switch
            {
                "APA" => GenerateApaInText(source),
                "MLA" => GenerateMlaInText(source),
                "CHICAGO" => GenerateChicagoInText(source),
                _ => GenerateApaInText(source)
            };
        }

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
            var ordered = sources.OrderBy(s => s.Author).ThenBy(s => s.Year).ToList();

            foreach (var source in ordered)
            {
                sb.AppendLine(GenerateCitation(source, style));
                sb.AppendLine(); // Add extra newline for spacing between entries
            }

            return sb.ToString().Trim();
        }

        public string UpdateBibliography(Note note)
        {
            if (note.Sources == null || !note.Sources.Any())
            {
                // Optionally remove bibliography if no sources?
                // For now, let's assume we just return content as is or remove the section.
                // But robust behavior might be to clear the section.
                // Let's implement removal if sources are empty.
                return RemoveBibliographySection(note.Content);
            }

            string style = note.CitationStyle ?? "APA";
            string header = GetBibliographyHeader(style);
            string bibContent = GenerateBibliography(note.Sources, style);

            string newSection = $"\n\n## {header}\n\n{bibContent}";

            // Regex to find existing bibliography section
            // Look for ## References, ## Works Cited, ## Bibliography
            // We assume it's at the end of the document.
            var pattern = @"(\n\n## (References|Works Cited|Bibliography)\s*[\s\S]*$)";

            if (Regex.IsMatch(note.Content, pattern))
            {
                return Regex.Replace(note.Content, pattern, newSection);
            }
            else
            {
                return note.Content + newSection;
            }
        }

        private string RemoveBibliographySection(string content)
        {
            var pattern = @"(\n\n## (References|Works Cited|Bibliography)\s*[\s\S]*$)";
            return Regex.Replace(content, pattern, "").TrimEnd();
        }

        private string GetBibliographyHeader(string style)
        {
            return style.ToUpper() switch
            {
                "MLA" => "Works Cited",
                "APA" => "References",
                "CHICAGO" => "Bibliography",
                _ => "References"
            };
        }

        // --- In-Text Generators ---

        private string GenerateApaInText(AcademicSource s)
        {
            // (Author, Year)
            string author = GetLastName(s.Author);
            if (string.IsNullOrEmpty(author)) author = s.Title; // Fallback

            return $"({author}, {s.Year})";
        }

        private string GenerateMlaInText(AcademicSource s)
        {
            // (Author Page) - Since we don't have page context here, usually (Author)
            string author = GetLastName(s.Author);
            if (string.IsNullOrEmpty(author)) author = s.Title;

            // Ideally we would append page number if known in context, but for generic generator:
            return $"({author})";
        }

        private string GenerateChicagoInText(AcademicSource s)
        {
            // (Author Year)
            string author = GetLastName(s.Author);
            if (string.IsNullOrEmpty(author)) author = s.Title;

            return $"({author} {s.Year})";
        }

        // --- Reference Generators ---

        private string GenerateApa(AcademicSource s)
        {
            // APA 7: Author, A. A. (Year). Title of work. *Source*. DOI/URL
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(s.Author))
            {
                sb.Append($"{s.Author} ");
            }
            else
            {
                sb.Append($"{s.Title}. ");
            }

            sb.Append($"({s.Year}). ");

            if (!string.IsNullOrEmpty(s.Author)) // If author exists, title follows
            {
                 // Check if it's an article (has container) or book
                if (!string.IsNullOrEmpty(s.ContainerTitle))
                {
                    // Article title not italicized
                    sb.Append($"{s.Title}. ");
                    // Container title italicized
                    sb.Append($"*{s.ContainerTitle}*");

                    if (!string.IsNullOrEmpty(s.Volume)) sb.Append($", *{s.Volume}*");
                    if (!string.IsNullOrEmpty(s.Issue)) sb.Append($"({s.Issue})");
                    if (!string.IsNullOrEmpty(s.Pages)) sb.Append($", {s.Pages}");

                    sb.Append(". ");
                }
                else
                {
                    // Book title italicized
                    sb.Append($"*{s.Title}*. ");
                    if (!string.IsNullOrEmpty(s.Publisher)) sb.Append($"{s.Publisher}. ");
                }
            }
            else
            {
                 if (!string.IsNullOrEmpty(s.ContainerTitle))
                {
                    sb.Append($"*{s.ContainerTitle}*");
                     if (!string.IsNullOrEmpty(s.Volume)) sb.Append($", *{s.Volume}*");
                    if (!string.IsNullOrEmpty(s.Issue)) sb.Append($"({s.Issue})");
                    if (!string.IsNullOrEmpty(s.Pages)) sb.Append($", {s.Pages}");
                    sb.Append(". ");
                }
                else
                {
                     if (!string.IsNullOrEmpty(s.Publisher)) sb.Append($"{s.Publisher}. ");
                }
            }

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
            // MLA 9: Author. "Title of Source." *Title of Container*, Other contributors, Version, Number, Publisher, Publication Date, Location.
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(s.Author))
            {
                sb.Append($"{s.Author}. ");
            }

            // Title
            if (!string.IsNullOrEmpty(s.ContainerTitle))
            {
                // Article in container -> Quotes
                sb.Append($"\"{s.Title}.\" ");
                sb.Append($"*{s.ContainerTitle}*");
            }
            else
            {
                // Standalone -> Italics
                sb.Append($"*{s.Title}*");
            }

            // Volume/Issue/Pages if applicable (Container)
            if (!string.IsNullOrEmpty(s.ContainerTitle))
            {
                 sb.Append(", ");
                 if (!string.IsNullOrEmpty(s.Volume)) sb.Append($"vol. {s.Volume}");
                 if (!string.IsNullOrEmpty(s.Issue)) sb.Append($", no. {s.Issue}");
            }
            else
            {
                sb.Append(".");
            }

            // Publisher
            if (!string.IsNullOrEmpty(s.Publisher))
            {
                sb.Append($" {s.Publisher},");
            }

            // Year
            sb.Append($" {s.Year}");

             if (!string.IsNullOrEmpty(s.ContainerTitle) && !string.IsNullOrEmpty(s.Pages))
            {
                 sb.Append($", pp. {s.Pages}");
            }

            sb.Append(".");

             // DOI/URL
             if (!string.IsNullOrEmpty(s.Doi))
            {
                sb.Append($" https://doi.org/{s.Doi}."); // MLA doesn't strictly require https://doi.org prefix in all versions but usually preferred
            }
            else if (!string.IsNullOrEmpty(s.Url))
            {
                sb.Append($" {s.Url}.");
            }


            return sb.ToString().Trim();
        }

        private string GenerateChicago(AcademicSource s)
        {
            // Chicago (Notes and Bibliography): Author. *Title of Book*. Place: Publisher, Year.
            // Or Author. "Title of Article." *Journal Name* Volume, no. Issue (Year): Pages.

            var sb = new StringBuilder();

             if (!string.IsNullOrEmpty(s.Author))
            {
                sb.Append($"{s.Author}. ");
            }

             if (!string.IsNullOrEmpty(s.ContainerTitle))
             {
                 // Article
                 sb.Append($"\"{s.Title}.\" ");
                 sb.Append($"*{s.ContainerTitle}* ");
                 if (!string.IsNullOrEmpty(s.Volume)) sb.Append($"{s.Volume}");
                 if (!string.IsNullOrEmpty(s.Issue)) sb.Append($", no. {s.Issue}");
                 sb.Append($" ({s.Year})");
                 if (!string.IsNullOrEmpty(s.Pages)) sb.Append($": {s.Pages}");
                 sb.Append(".");
             }
             else
             {
                 // Book
                 sb.Append($"*{s.Title}*. ");
                 // Place is often omitted in modern simplified engines unless we add City field.
                 if (!string.IsNullOrEmpty(s.Publisher)) sb.Append($"{s.Publisher}, ");
                 sb.Append($"{s.Year}.");
             }

            return sb.ToString().Trim();
        }

        private string GetLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) return parts.Last().Replace(",", "");
            return fullName;
        }
    }
}
