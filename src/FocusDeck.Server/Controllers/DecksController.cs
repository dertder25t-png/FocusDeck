using Microsoft.AspNetCore.Mvc;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DecksController : ControllerBase
    {
        private readonly AutomationDbContext _context;

        public DecksController(AutomationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Deck>>> GetDecks()
        {
            var decks = await _context.Decks
                .AsNoTracking()
                .ToListAsync();
            return Ok(decks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Deck>> GetDeck(Guid id)
        {
            var deck = await _context.Decks.FindAsync(id);
            if (deck == null)
            {
                return NotFound();
            }
            return Ok(deck);
        }

        [HttpPost]
        public async Task<ActionResult<Deck>> CreateDeck([FromBody] Deck newDeck)
        {
            if (newDeck == null)
            {
                return BadRequest("Deck object is null");
            }

            // Generate ID if missing
            if (newDeck.Id == Guid.Empty)
            {
                newDeck.Id = Guid.NewGuid();
            }

            // TenantId handled automatically by DbContext/Middleware
            _context.Decks.Add(newDeck);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeck), new { id = newDeck.Id }, newDeck);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeck(Guid id, [FromBody] Deck updatedDeck)
        {
            if (updatedDeck == null || id != updatedDeck.Id)
            {
                return BadRequest("Invalid deck data");
            }

            var deck = await _context.Decks.FindAsync(id);
            if (deck == null)
            {
                return NotFound();
            }

            deck.Name = updatedDeck.Name;
            deck.Cards = updatedDeck.Cards;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeck(Guid id)
        {
            var deck = await _context.Decks.FindAsync(id);
            if (deck == null)
            {
                return NotFound();
            }

            _context.Decks.Remove(deck);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
