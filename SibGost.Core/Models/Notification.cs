using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int BookingId {  get; set; }
        public Booking Booking { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime ScheduledAt { get; set; }
    }
}
