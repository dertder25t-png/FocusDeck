using Microsoft.AspNetCore.Mvc;
using FocusDeck.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DecksController : ControllerBase
    {
        private static readonly List<Deck> _decks = new List<Deck>();

        [HttpGet]
        public ActionResult<IEnumerable<Deck>> GetDecks()
        {
            return Ok(_decks);
        }

        [HttpGet("{id}")]
        public ActionResult<Deck> GetDeck(Guid id)
        {
            var deck = _decks.FirstOrDefault(d => d.Id == id);
            if (deck == null)
            {
                return NotFound();
            }
            return Ok(deck);
        }

        [HttpPost]
        public ActionResult<Deck> CreateDeck([FromBody] Deck newDeck)
        {
            if (newDeck == null)
            {
                return BadRequest("Deck object is null");
            }
            newDeck.Id = Guid.NewGuid();
            _decks.Add(newDeck);
            return CreatedAtAction(nameof(GetDeck), new { id = newDeck.Id }, newDeck);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateDeck(Guid id, [FromBody] Deck updatedDeck)
        {
            if (updatedDeck == null || id != updatedDeck.Id)
            {
                return BadRequest("Invalid deck data");
            }

            var deck = _decks.FirstOrDefault(d => d.Id == id);
            if (deck == null)
            {
                return NotFound();
            }

            deck.Name = updatedDeck.Name;
            deck.Cards = updatedDeck.Cards;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteDeck(Guid id)
        {
            var deck = _decks.FirstOrDefault(d => d.Id == id);
            if (deck == null)
            {
                return NotFound();
            }

            _decks.Remove(deck);
            return NoContent();
        }
    }
}
