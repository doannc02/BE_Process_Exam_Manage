using ExamProcessManage.Helpers;
using ExamProcessManage.Models;

namespace ExamProcessManage.Dtos
{
    public partial class TeacherProposalDTO
    {
        public int id { get; set; }
        public CommonObject? porposal { get; set; }
        public CommonObject? user { get; set; }
        public virtual Proposal? Proposal { get; set; }
        public virtual User? User { get; set; }
    }
}
