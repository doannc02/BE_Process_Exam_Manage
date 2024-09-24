using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class Teacher
    {
        public Teacher()
        {
            Users = new HashSet<User>();
        }

        public ulong Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Position { get; set; }
        public string? Address { get; set; }
        public string? LinkFacebook { get; set; }
        public string? LinkInsta { get; set; }
        public string? ProgressTeach { get; set; }
        public string? ReseachArea { get; set; }
        public string? CoverPath { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}
