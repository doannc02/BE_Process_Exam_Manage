namespace ExamProcessManage.Models
{
    public partial class Proposal
    {
        public Proposal()
        {
            ExamSets = new HashSet<ExamSet>();
            TeacherProposals = new HashSet<TeacherProposal>();
        }

        public int ProposalId { get; set; }
        public string PlanCode { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public string Semester { get; set; } = null!;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Content { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly? CreateAt { get; set; }
        public DateOnly? UpdateAt { get; set; }

        public bool? IsCreatedByAdmin { get; set; }

        public virtual ICollection<ExamSet> ExamSets { get; set; }
        public virtual ICollection<TeacherProposal> TeacherProposals { get; set; }
    }
}
