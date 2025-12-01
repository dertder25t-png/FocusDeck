using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities
{
    public class Deck : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public List<string>? Cards { get; set; }
        public Guid TenantId { get; set; }
    }
}
