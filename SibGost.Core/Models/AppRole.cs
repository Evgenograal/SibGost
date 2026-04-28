using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Models
{
    public class AppRole
    {
        public int Id { get; set;  }
        public string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
