using ExamProcessManage.Helpers;

namespace ExamProcessManage.RequestModels
{
    public class ExamRequestParams : QueryObject
    {    
        public string? status {  get; set; }
        public int? academic_year_id { get; set; }
        public int? month_upload { get; set; }

        public int? exam_set_id { get; set; }
    }
}
