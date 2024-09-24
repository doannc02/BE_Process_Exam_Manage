using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class Permission
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
