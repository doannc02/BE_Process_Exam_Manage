using ExamProcessManage.Helpers;
using ExamProcessManage.Models;

namespace ExamProcessManage.Dtos
{
    public class ExamDTO
    {
        public int exam_id { get; set; }
        public string exam_code { get; set; } = null!;
        public string? exam_name { get; set; }
        public string? attached_file { get; set; }
        public string? comment { get; set; }
        public string? description { get; set; }
        public DateOnly? upload_date { get; set; }
        public string status { get; set; } = null!;
        public virtual CommonObject? academic_year { get; set; }
        public virtual CommonObject? exam_set { get; set; }
    }
}
