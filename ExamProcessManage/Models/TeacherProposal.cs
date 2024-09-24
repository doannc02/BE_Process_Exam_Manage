using System;
using System.Collections.Generic;

namespace ExamProcessManage.Models
{
    public partial class TeacherProposal
    {
        public int Id { get; set; }
        public int? ProposalId { get; set; }
        public ulong? UserId { get; set; }

        public virtual Proposal? Proposal { get; set; }
        public virtual User? User { get; set; }
    }
}
