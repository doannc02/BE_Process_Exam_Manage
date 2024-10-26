using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ExamProcessManage.Helpers
{
    public class CustomJsonResult : IActionResult
    {
        private readonly int _status;
        private readonly string _traceId;
        private readonly string _title;
        private readonly List<ErrorDetail>? _errorDetail;

        public CustomJsonResult(int status, HttpContext httpContext, string title, List<ErrorDetail>? errorDetails = null)
        {
            _status = status;
            _traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            _title = title;
            _errorDetail = errorDetails;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = _status;
            response.ContentType = "application/json";

            var result = new
            {
                status = _status,
                title = _title,
                traceId = _traceId,
                errors = _errorDetail
            };

            await response.WriteAsJsonAsync(result);
        }
    }
}
