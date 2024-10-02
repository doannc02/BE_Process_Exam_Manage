using ExamProcessManage.Helpers;

namespace ExamProcessManage.ResponseModels
{
    public class CourseReponse
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public int credit { get; set; }
        public CommonObject major { get; set; }
    }
}
