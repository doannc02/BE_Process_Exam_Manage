namespace ExamProcessManage.Models
{
    public partial class AcademicYear
    {
        public AcademicYear()
        {
            Exams = new HashSet<Exam>();
        }

        public int? AcademicYearId { get; set; }
        public string YearName { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Exam> Exams { get; set; }
    }
}