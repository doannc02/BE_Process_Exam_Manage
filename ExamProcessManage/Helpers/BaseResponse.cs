namespace ExamProcessManage.Helpers
{
    public class BaseResponse<T>
    {
        public string message { get; set; }
        public T data { get; set; }
    }

     public class DetailResponse {
        public int? id { get; set; }
     }
    public class BaseResponseDetail : BaseResponse<DetailResponse>
    {
    }
}
