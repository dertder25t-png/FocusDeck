using System;
using System.Collections.Generic;

namespace FocusDeck.Server.Models
{
    public class Deck
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<string> Cards { get; set; }
    }
}
