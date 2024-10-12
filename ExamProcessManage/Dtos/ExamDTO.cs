using ExamProcessManage.Helpers;

namespace ExamProcessManage.Dtos
{
    public class ExamDTO
    {
        public int? id { get; set; }
        public string code { get; set; } = null!;
        public string? name { get; set; }
        public string? attached_file { get; set; }
        public string? comment { get; set; }
        public string? description { get; set; }
        public string? upload_date { get; set; }
        public string status { get; set; } = null!;
        public Boolean? isGetForAddExamSet { get; set; } = false;
        public  object? user { get; set; }
        public virtual CommonObject? academic_year { get; set; }
        public virtual CommonObject? exam_set { get; set; }
    }
}
