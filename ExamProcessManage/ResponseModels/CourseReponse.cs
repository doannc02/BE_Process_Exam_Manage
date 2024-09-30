using ExamProcessManage.Helpers;

namespace ExamProcessManage.ResponseModels
{
    public class CourseReponse
    {
        public int course_id { get; set; }
        public string course_code { get; set; }
        public string course_name { get; set; }
        public int course_credit { get; set; }
        public CommonObject major { get; set; }
    }
}
