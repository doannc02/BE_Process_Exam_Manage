using ExamProcessManage.Helpers;

namespace ExamProcessManage.ResponseModels
{
    public class MajorResponse
    {
        public int major_id { get; set; }
        public string major_name { get; set; }
        public CommonObject department { get; set; }
    }
}
