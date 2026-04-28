using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;

namespace SibGost.Core.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Login {  get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public AppRole Role { get; set; }
        public ICollection<Booking> Bookings {  get; set; } = new List<Booking>();
    }
}
