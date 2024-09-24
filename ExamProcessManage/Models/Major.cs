using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class Major
    {
        public Major()
        {
            Courses = new HashSet<Course>();
        }

        public int MajorId { get; set; }
        public string? MajorName { get; set; }
        public int? DepartmentId { get; set; }

        public virtual Department? Department { get; set; }
        public virtual ICollection<Course> Courses { get; set; }
    }
}
