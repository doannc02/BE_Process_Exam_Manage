using ExamProcessManage.Helpers;
using ExamProcessManage.Models;

namespace ExamProcessManage.Dtos
{
    public partial class ProposalDTO
    {
        public ProposalDTO()
        {
            exam_sets = new HashSet<ExamSetDTO>();
            teacher_roposals = new HashSet<TeacherProposalDTO>();
        }

        public int? id { get; set; }
        public string code { get; set; } = null!;
        public string semester { get; set; } = null!;
        public string? start_date { get; set; }
        public string? end_date { get; set; }
        public string? content { get; set; }
        public string status { get; set; } = null!;
        public  CommonObject academic_year { get; set; } = null!;
        public CommonObject user { get; set; }
        public int? total_exam_set { get; set; }
        public virtual ICollection<ExamSetDTO>? exam_sets { get; set; }
        public virtual ICollection<TeacherProposalDTO>? teacher_roposals { get; set; }
    }
}
