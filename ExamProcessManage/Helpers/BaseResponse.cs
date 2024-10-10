using ExamProcessManage.Dtos;

namespace ExamProcessManage.Helpers
{
    public class BaseResponse<T>
    {
        public int? errorCode { get; set; }
        public string? traceId { get; set; }
        public string? message { get; set; }
        public T data { get; set; }
        public IEnumerable<ErrorDetail>? errs { get; set; }
    }

    public class DetailResponse
    {
        public int? id { get; set; }
    }

    public class BaseResponseId : BaseResponse<DetailResponse> { }
}
