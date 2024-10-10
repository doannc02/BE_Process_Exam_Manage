using ExamProcessManage.Dtos;

namespace ExamProcessManage.Helpers
{
    public class ErrorResponse
    {
        public string traceId { get; set; }
        public string message { get; set; }
        public List<ErrorDetail> error { get; set; }
    }

    public class ErrorDetail
    {
        public string field { get; set; }
        public string message { get; set; }
    }
}
