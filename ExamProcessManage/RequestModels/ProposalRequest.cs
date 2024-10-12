using ExamProcessManage.Helpers;

namespace ExamProcessManage.RequestModels
{
    public class ProposalRequest
    {
        public int? id { get; set; }
        public string code { get; set; } = null!;
        public int user_id { get; set; }
        public CommonObject academic_year { get; set; } = null!;
        public string semester { get; set; } = null!;
        public DateOnly? start_date { get; set; }
        public DateOnly? end_date { get; set; }
        public string? content { get; set; }
        public string status { get; set; } = null!;
    }
}
