using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class Department
    {
        public Department()
        {
            Majors = new HashSet<Major>();
        }

        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        public virtual ICollection<Major> Majors { get; set; }
    }
}
