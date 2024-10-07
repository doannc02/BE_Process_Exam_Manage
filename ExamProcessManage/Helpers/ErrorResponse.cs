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
        public List<string> fields { get; set; }  
        public string message { get; set; }  
    }

}