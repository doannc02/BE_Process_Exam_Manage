namespace ExamProcessManage.Models
{
    public partial class Exam
    {
        public int ExamId { get; set; }
        public int? CreatorId { get; set; }
        public string ExamCode { get; set; } = null!;
        public string? ExamName { get; set; }
        public int? ExamSetId { get; set; }
        public int? AcademicYearId { get; set; }
        public string? AttachedFile { get; set; }
        public string? Comment { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly? CreateAt { get; set; }
        public DateOnly? UpdateAt { get; set; }

        public virtual AcademicYear? AcademicYear { get; set; }
        public virtual ExamSet? ExamSet { get; set; }
    }
}
