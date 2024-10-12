namespace ExamProcessManage.Helpers
{
    public class BaseResponse<T>
    {
        public int? status { get; set; }
        public string? traceId { get; set; }
        public string? message { get; set; }
        public T data { get; set; }
        public List<ErrorDetail>? errors { get; set; }
    }

    public class DetailResponse
    {
        public int? id { get; set; }
    }

    public class BaseResponseId : BaseResponse<DetailResponse> { }
}
