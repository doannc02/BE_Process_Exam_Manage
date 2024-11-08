using ExamProcessManage.Helpers;

namespace ExamProcessManage.ResponseModels
{
    public class MajorResponse
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string? created_at { get; set; }
        public string? updated_at { get; set; }
        public CommonObject department { get; set; }
    }
}