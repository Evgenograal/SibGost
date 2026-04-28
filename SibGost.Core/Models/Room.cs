using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int Floor {  get; set; }
        public string ImgUrl { get; set; }
        public string Status { get; set; } = "Free";
        public bool IsActive { get; set; } = true;
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public ICollection<RoomAmenity> RoomAmenities { get; set; } = new List<RoomAmenity>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    } 
}
