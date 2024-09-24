using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class Exam
    {
        public int ExamId { get; set; }
        public string ExamCode { get; set; } = null!;
        public string? ExamName { get; set; }
        public int? ExamSetId { get; set; }
        public int? AcademicYearId { get; set; }
        public string? AttachedFile { get; set; }
        public string? Comment { get; set; }
        public string? Description { get; set; }
        public DateOnly? UploadDate { get; set; }
        public string Status { get; set; } = null!;

        public virtual AcademicYear? AcademicYear { get; set; }
        public virtual ExamSet? ExamSet { get; set; }
    }
}
