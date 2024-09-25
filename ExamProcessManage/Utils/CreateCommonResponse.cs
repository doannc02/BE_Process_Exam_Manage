using ExamProcessManage.Helpers;
using System.Diagnostics;

namespace ExamProcessManage.Utils
{
    public class CreateCommonResponse
    {
        public CommonResponse<T> CreateResponse<T>(string message, HttpContext httpContext, T data)
        {
            return new CommonResponse<T>
            {
                message = message,
                traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier,
                data = data
            };
        }
    }
}
