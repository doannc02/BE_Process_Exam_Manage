using ExamProcessManage.Helpers;
using ExamProcessManage.Models;

namespace ExamProcessManage.Dtos
{
    public partial class ProposalDTO
    {
        public ProposalDTO()
        {
            exam_sets = new HashSet<ExamSetDTO>();
            teacher_roposals = new HashSet<TeacherProposal>();
        }

        public CommonObject user { get; set; }
        public int proposal_id { get; set; }
        public string plan_code { get; set; } = null!;
        public string academic_year { get; set; } = null!;
        public string semester { get; set; } = null!;
        public DateOnly? start_date { get; set; }
        public DateOnly? end_date { get; set; }
        public string? content { get; set; }
        public string status { get; set; } = null!;

        public virtual ICollection<ExamSetDTO>? exam_sets { get; set; }
        public virtual ICollection<TeacherProposal>? teacher_roposals { get; set; }
    }
}
