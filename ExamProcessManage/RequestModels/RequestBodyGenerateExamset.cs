namespace ExamProcessManage.RequestModels
{
    public class RequestBodyGenerateExamset
    {
        public int academic_year_id { get; set; }
        public IEnumerable<int> exam_set_ids { get; set; }
        public string? state_exam_set { get; set; }
    }
}
