using ExamProcessManage.Helpers;

namespace ExamProcessManage.ResponseModels
{
    public class MajorResponse
    {
        public int id { get; set; }
        public string name { get; set; }
        public CommonObject department { get; set; }
    }
}
