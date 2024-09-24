using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class RolesPermission
    {
        public ulong RoleId { get; set; }
        public ulong PermissionId { get; set; }

        public virtual Permission Permission { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
    }
}
