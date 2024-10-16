﻿namespace ExamProcessManage.Models
{
    public partial class ExamSet
    {
        public ExamSet()
        {
            Exams = new HashSet<Exam>();
        }

        public int ExamSetId { get; set; }
        public int? CreatorId { get; set; }
        public string? ExamSetName { get; set; }
        public int? DepartmentId { get; set; } = null!;
        public int? MajorId { get; set; } = null!;
        public int ExamQuantity { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public int? CourseId { get; set; }
        public int? ProposalId { get; set; }
        public DateOnly? CreateAt { get; set; }
        public DateOnly? UpdateAt { get; set; }

        public virtual Course? Course { get; set; }
        public virtual Proposal? Proposal { get; set; }
        public virtual ICollection<Exam> Exams { get; set; }
    }
}
