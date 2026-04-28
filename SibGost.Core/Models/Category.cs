using System.Globalization;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<PricePolicy> PricePolicies { get; set; } = new List<PricePolicy>();

        public string DisplayPrice => $"{BasePrice:N0} ₽ / ночь";

        public decimal CalculatePriceForDate(DateTime date)
        {
            var policy = PricePolicies?.FirstOrDefault(p =>
                p.IsActive &&
                p.StartDate.Date <= date.Date &&
                p.EndDate.Date >= date.Date);

            return BasePrice * (policy?.Multiplier ?? 1.0m);
        }
    }
}
