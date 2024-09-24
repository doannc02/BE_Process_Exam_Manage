using ExamProcessManage.Helpers;

namespace ExamProcessManage.ResponseModels
{
    public class ProposaResponse
    {
        public string academic_year { get; set; }
        public CommonObject instructor { get; set; }
        public string id { get; set; }
        public CommonObject user { get; set; }
        public CommonObject course { get; set; }
        public string status { get; set; }
        public string deadline { get; set; }
        public string start { get; set; }
        public int number_of_assignment { get; set; }
        public string semester { get; set; }
        public IEnumerable<AssignmentUploadResponse> list_exams { get; set;}
    }
}
