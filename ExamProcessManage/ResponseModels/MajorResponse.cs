using ExamProcessManage.Helpers;

namespace ExamProcessManage.ResponseModels
{
    public class MajorResponse
    {
        public int? id { get; set; }
        public string name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public CommonObject department { get; set; }
    }
}