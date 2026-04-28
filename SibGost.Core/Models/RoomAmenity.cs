using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class RoomAmenity
    {
        public int RoomId {  get; set; }
        public Room Room { get; set; }
        public int AmenityId { get; set; }
        public Amenity Amenity { get; set; }
    }
}
