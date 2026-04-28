using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class Amenity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description {  get; set; }
        ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
    }
}
