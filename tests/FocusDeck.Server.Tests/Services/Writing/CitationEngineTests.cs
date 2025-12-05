using System;
using System.Collections.Generic;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.Writing;
using Xunit;

namespace FocusDeck.Server.Tests.Services.Writing
{
    public class CitationEngineTests
    {
        private readonly CitationEngine _engine;

        public CitationEngineTests()
        {
            _engine = new CitationEngine();
        }

        [Fact]
        public void GenerateInTextCitation_Apa_ReturnsCorrectFormat()
        {
            var source = new AcademicSource
            {
                Author = "John Smith",
                Year = 2023,
                Title = "The Future of AI"
            };

            var result = _engine.GenerateInTextCitation(source, "APA");

            Assert.Equal("(Smith, 2023)", result);
        }

        [Fact]
        public void GenerateInTextCitation_Mla_ReturnsCorrectFormat()
        {
            var source = new AcademicSource
            {
                Author = "Jane Doe",
                Year = 2022,
                Title = "Modern Computing"
            };

            var result = _engine.GenerateInTextCitation(source, "MLA");

            Assert.Equal("(Doe)", result);
        }

        [Fact]
        public void GenerateCitation_Apa_Article_ReturnsCorrectFormat()
        {
            var source = new AcademicSource
            {
                Author = "Alice Researcher",
                Year = 2024,
                Title = "Deep Learning Trends",
                ContainerTitle = "Journal of AI",
                Volume = "10",
                Issue = "2",
                Pages = "100-110",
                Doi = "10.1234/jai.2024.10.2"
            };

            var result = _engine.GenerateCitation(source, "APA");

            // APA: Author. (Year). Title. *Journal*, *Vol*(Issue), Pages. https://doi...
            // Checking key components
            Assert.Contains("Researcher", result);
            Assert.Contains("(2024)", result);
            Assert.Contains("Deep Learning Trends", result);
            Assert.Contains("*Journal of AI*", result);
            Assert.Contains("*10*(2)", result); // Simple check for formatting
            Assert.Contains("100-110", result);
            Assert.Contains("https://doi.org/10.1234/jai.2024.10.2", result);
        }

        [Fact]
        public void GenerateCitation_Mla_Book_ReturnsCorrectFormat()
        {
             var source = new AcademicSource
            {
                Author = "Bob Builder",
                Year = 2020,
                Title = "Building Strong Foundations",
                Publisher = "Construction Press"
            };

            var result = _engine.GenerateCitation(source, "MLA");

            // MLA: Author. *Title*. Publisher, Year.
            Assert.Contains("Builder.", result);
            Assert.Contains("*Building Strong Foundations*", result);
            Assert.Contains("Construction Press,", result);
            Assert.Contains("2020.", result);
        }

        [Fact]
        public void UpdateBibliography_AppendsNewBibliography_WhenNoneExists()
        {
            var note = new Note
            {
                Content = "This is a paper.",
                CitationStyle = "APA",
                Sources = new List<AcademicSource>
                {
                    new AcademicSource { Author = "A. Author", Year = 2020, Title = "Book A" },
                    new AcademicSource { Author = "B. Author", Year = 2021, Title = "Book B" }
                }
            };

            var updatedContent = _engine.UpdateBibliography(note);

            Assert.Contains("This is a paper.", updatedContent);
            Assert.Contains("## References", updatedContent);
            Assert.Contains("Book A", updatedContent);
            Assert.Contains("Book B", updatedContent);
        }

         [Fact]
        public void UpdateBibliography_ReplacesExistingBibliography()
        {
            var note = new Note
            {
                Content = "This is a paper.\n\n## References\n\nOld Reference 1",
                CitationStyle = "APA",
                Sources = new List<AcademicSource>
                {
                    new AcademicSource { Author = "New Author", Year = 2023, Title = "New Book" }
                }
            };

            var updatedContent = _engine.UpdateBibliography(note);

            Assert.Contains("This is a paper.", updatedContent);
            Assert.Contains("## References", updatedContent);
            Assert.Contains("New Book", updatedContent);
            Assert.DoesNotContain("Old Reference 1", updatedContent);
        }

        [Fact]
        public void UpdateBibliography_SwitchesHeader_WhenStyleChanges()
        {
             var note = new Note
            {
                Content = "Content.\n\n## References\n\nOld Ref",
                CitationStyle = "MLA", // Should switch to "Works Cited"
                Sources = new List<AcademicSource>
                {
                    new AcademicSource { Author = "M. Author", Year = 2023, Title = "MLA Book" }
                }
            };

            // Note: The current implementation searches for existing "References" OR "Works Cited" and replaces it with the NEW header based on style.
            // Since the existing content has "## References", the regex matches "## References...".
            // The new content will have "## Works Cited...".

            var updatedContent = _engine.UpdateBibliography(note);

            Assert.Contains("## Works Cited", updatedContent);
            Assert.DoesNotContain("## References", updatedContent); // Should be replaced
            Assert.Contains("MLA Book", updatedContent);
        }
    }
}
