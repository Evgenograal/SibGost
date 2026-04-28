using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class PricePolicy
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Multiplier { get; set; } = 1.0m;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
