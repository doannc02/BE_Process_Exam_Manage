using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class User
    {
        public User()
        {
            TeacherProposals = new HashSet<TeacherProposal>();
        }

        public ulong Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? AvatarPath { get; set; }
        public string? TokenProcess { get; set; }
        public string? RememberToken { get; set; }
        public ulong? TeacherId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public ulong RoleId { get; set; }

        public virtual Role Role { get; set; } = null!;
        public virtual Teacher? Teacher { get; set; }
        public virtual ICollection<TeacherProposal> TeacherProposals { get; set; }
    }
}
