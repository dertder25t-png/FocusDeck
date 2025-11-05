using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

[Authorize]
[ApiController]
[Route("v1/design")]
public class DesignController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<DesignController> _logger;

    public DesignController(AutomationDbContext db, ILogger<DesignController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

    [HttpGet("projects")]
    public async Task<IActionResult> ListProjects([FromQuery] int page = 1, [FromQuery] int perPage = 20)
    {
        var userId = GetUserId();
        var skip = (page - 1) * perPage;

        var projects = await _db.DesignProjects
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(perPage)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.GoalsText,
                p.Vibes,
                p.RequirementsText,
                p.BrandKeywords,
                p.CreatedAt,
                p.UpdatedAt,
                IdeaCount = p.Ideas.Count,
                PinnedCount = p.Ideas.Count(i => i.IsPinned)
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var userId = GetUserId();

        var project = new DesignProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            GoalsText = request.GoalsText,
            Vibes = request.Vibes ?? new List<string>(),
            RequirementsText = request.RequirementsText,
            BrandKeywords = request.BrandKeywords ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        _db.DesignProjects.Add(project);
        await _db.SaveChangesAsync();

        return Ok(new { project.Id, project.Title, project.CreatedAt });
    }

    [HttpGet("projects/{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var userId = GetUserId();

        var project = await _db.DesignProjects
            .Include(p => p.Ideas)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null)
            return NotFound();

        return Ok(new
        {
            project.Id,
            project.Title,
            project.GoalsText,
            project.Vibes,
            project.RequirementsText,
            project.BrandKeywords,
            project.CreatedAt,
            project.UpdatedAt,
            Ideas = project.Ideas.OrderByDescending(i => i.CreatedAt).Select(i => new
            {
                i.Id,
                Type = i.Type.ToString(),
                i.Content,
                i.AssetId,
                i.Score,
                i.IsPinned,
                i.CreatedAt
            })
        });
    }

    [HttpPost("projects/{id}/generate")]
    public async Task<IActionResult> GenerateIdeas(Guid id, [FromBody] GenerateRequest request)
    {
        var userId = GetUserId();

        var project = await _db.DesignProjects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null)
            return NotFound();

        // Generate mock ideas based on mode
        var ideas = new List<DesignIdea>();
        var now = DateTime.UtcNow;

        if (request.Mode == "thumbnails" || request.Mode == "all")
        {
            // Generate 12 thumbnail/layout ideas
            var layouts = new[]
            {
                "Hero-centered layout: Large focal image with title overlay, 2-column grid below",
                "Magazine-style: Asymmetric grid, bold typography, sidebar navigation",
                "Minimalist: Whitespace-dominant, single-column, serif headers",
                "Card-based: Equal-size cards in 3x4 grid, rounded corners",
                "Split-screen: 50/50 image-text division, diagonal separator",
                "F-pattern: Traditional web layout, top nav, left sidebar, main content",
                "Z-pattern: Eye flow from top-left to bottom-right, CTA bottom-right",
                "Brutalist: Raw, geometric, monospace typography, stark contrast",
                "Dashboard: Header metrics, chart grid, sidebar filters",
                "Portfolio: Full-bleed images, overlay text, horizontal scroll",
                "Landing: Hero section, feature blocks, testimonials, CTA footer",
                "Blog: Featured post, 2-column grid, tag cloud sidebar"
            };

            for (int i = 0; i < 12; i++)
            {
                ideas.Add(new DesignIdea
                {
                    Id = Guid.NewGuid(),
                    ProjectId = id,
                    Type = DesignIdeaType.Thumbnail,
                    Content = layouts[i % layouts.Length],
                    Score = 0.7 + (i % 3) * 0.1,
                    CreatedAt = now.AddSeconds(i)
                });
            }
        }

        if (request.Mode == "concepts" || request.Mode == "all")
        {
            // Generate 8 concept ideas
            var concepts = new[]
            {
                "**Palette**: Deep navy (#1A237E), coral (#FF6B6B), cream (#F5F5DC)\n**Typography**: Playfair Display + Open Sans\n**System**: 8pt grid, 12px radii, shadows 0/2/4/8",
                "**Palette**: Forest green (#2D5016), gold (#D4AF37), ivory (#FFFFF0)\n**Typography**: Crimson Text + Lato\n**System**: Golden ratio, 16px radii, organic shapes",
                "**Palette**: Charcoal (#36454F), electric blue (#0ABAB5), white (#FFFFFF)\n**Typography**: Roboto Mono + Inter\n**System**: 4pt grid, sharp edges, flat design",
                "**Palette**: Terracotta (#E07A5F), sage (#81B29A), sand (#F4F1DE)\n**Typography**: Merriweather + Source Sans Pro\n**System**: Earthy tones, soft shadows, warm aesthetic",
                "**Palette**: Midnight (#191970), rose (#FFB3D9), silver (#C0C0C0)\n**Typography**: Montserrat + Raleway\n**System**: Gradient overlays, glassmorphism, modern",
                "**Palette**: Burgundy (#800020), mustard (#FFDB58), cream (#FFFDD0)\n**Typography**: Georgia + Helvetica\n**System**: Classic proportions, serif dominance",
                "**Palette**: Slate (#708090), lime (#32CD32), off-white (#FAF9F6)\n**Typography**: Poppins + Nunito\n**System**: Rounded, playful, high contrast",
                "**Palette**: Obsidian (#0B1215), cyan (#00FFFF), pearl (#EAE0C8)\n**Typography**: Futura + Gill Sans\n**System**: Sci-fi aesthetic, neon accents, dark mode"
            };

            for (int i = 0; i < 8; i++)
            {
                ideas.Add(new DesignIdea
                {
                    Id = Guid.NewGuid(),
                    ProjectId = id,
                    Type = DesignIdeaType.Prompt,
                    Content = concepts[i],
                    Score = 0.8 + (i % 2) * 0.05,
                    CreatedAt = now.AddSeconds(12 + i)
                });
            }
        }

        if (request.Mode == "references" || request.Mode == "all")
        {
            // Generate 10 reference leads
            var references = new[]
            {
                "**Swiss International Style** (1950s-60s)\n*Artists*: Josef Müller-Brockmann, Armin Hofmann\n*Keywords*: Grid systems, sans-serif, asymmetry\n*License*: Public domain imagery available",
                "**Bauhaus Movement** (1919-33)\n*Artists*: László Moholy-Nagy, Herbert Bayer\n*Keywords*: Geometric forms, primary colors, functionalism\n*License*: Many works in public domain",
                "**Art Deco** (1920s-30s)\n*Keywords*: Geometric patterns, luxury, streamlined\n*Reference*: Chrysler Building, Tamara de Lempicka\n*License*: Public domain (pre-1928)",
                "**Brutalism (Digital)** (2010s-present)\n*Artists*: Pascal Deville, Experimental Jetset\n*Keywords*: Raw HTML, monospace, high contrast\n*License*: CC BY-SA (some)",
                "**Memphis Design** (1980s)\n*Artists*: Ettore Sottsass, Nathalie du Pasquier\n*Keywords*: Bold geometry, bright colors, patterns\n*License*: Some public domain, check per work",
                "**Minimalism** (1960s-present)\n*Artists*: Dieter Rams, Donald Judd\n*Keywords*: Less is more, whitespace, functionality\n*License*: Varies, many CC0 derivatives",
                "**Psychedelic Art** (1960s-70s)\n*Artists*: Peter Max, Wes Wilson\n*Keywords*: Swirls, vibrant colors, optical illusions\n*License*: Public domain (pre-1978 works)",
                "**Constructivism** (1915-30s)\n*Artists*: El Lissitzky, Alexander Rodchenko\n*Keywords*: Geometric, bold typography, propaganda\n*License*: Public domain (Soviet era)",
                "**Vaporwave Aesthetic** (2010s)\n*Keywords*: Retro-futurism, gradients, glitch art, pastels\n*Reference*: Blank Banshee, MACINTOSH PLUS\n*License*: Public domain samples, CC BY",
                "**Material Design** (2014-present)\n*Source*: Google Material Design\n*Keywords*: Elevation, motion, bold color\n*License*: Apache 2.0 (design system)"
            };

            for (int i = 0; i < 10; i++)
            {
                ideas.Add(new DesignIdea
                {
                    Id = Guid.NewGuid(),
                    ProjectId = id,
                    Type = DesignIdeaType.Reference,
                    Content = references[i],
                    Score = 0.75 + (i % 4) * 0.05,
                    CreatedAt = now.AddSeconds(20 + i)
                });
            }
        }

        _db.DesignIdeas.AddRange(ideas);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Generated {Count} ideas for project {ProjectId}", ideas.Count, id);

        return Ok(new { Count = ideas.Count, Ideas = ideas.Select(i => new { i.Id, Type = i.Type.ToString(), i.Content, i.Score }) });
    }

    [HttpGet("projects/{id}/ideas")]
    public async Task<IActionResult> GetIdeas(Guid id, [FromQuery] string? type = null, [FromQuery] bool? pinned = null)
    {
        var userId = GetUserId();

        var project = await _db.DesignProjects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null)
            return NotFound();

        var query = _db.DesignIdeas.Where(i => i.ProjectId == id);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<DesignIdeaType>(type, true, out var ideaType))
            query = query.Where(i => i.Type == ideaType);

        if (pinned.HasValue)
            query = query.Where(i => i.IsPinned == pinned.Value);

        var ideas = await query
            .OrderByDescending(i => i.IsPinned)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                Type = i.Type.ToString(),
                i.Content,
                i.AssetId,
                i.Score,
                i.IsPinned,
                i.CreatedAt
            })
            .ToListAsync();

        return Ok(ideas);
    }

    [HttpPost("ideas/{id}/pin")]
    public async Task<IActionResult> TogglePin(Guid id)
    {
        var userId = GetUserId();

        var idea = await _db.DesignIdeas
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == id && i.Project.UserId == userId);

        if (idea == null)
            return NotFound();

        idea.IsPinned = !idea.IsPinned;
        await _db.SaveChangesAsync();

        return Ok(new { idea.Id, idea.IsPinned });
    }

    [HttpDelete("ideas/{id}")]
    public async Task<IActionResult> DeleteIdea(Guid id)
    {
        var userId = GetUserId();

        var idea = await _db.DesignIdeas
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Id == id && i.Project.UserId == userId);

        if (idea == null)
            return NotFound();

        _db.DesignIdeas.Remove(idea);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateProjectRequest(string Title, string? GoalsText, List<string>? Vibes, string? RequirementsText, List<string>? BrandKeywords);
public record GenerateRequest(string Mode); // "thumbnails", "concepts", "references", or "all"
