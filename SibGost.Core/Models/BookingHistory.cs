using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class BookingHistory
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; }
        public string Action { get; set; }
        public string ChangeBy { get; set; }
        public DateTime ChangeAt { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
