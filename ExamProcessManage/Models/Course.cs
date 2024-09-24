using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class Course
    {
        public Course()
        {
            ExamSets = new HashSet<ExamSet>();
        }

        public int CourseId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public int? CourseCredit { get; set; }
        public int? MajorId { get; set; }

        public virtual Major? Major { get; set; }
        public virtual ICollection<ExamSet> ExamSets { get; set; }
    }
}
