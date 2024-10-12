using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

            if (_status == StatusCodes.Status401Unauthorized ||
                _status == StatusCodes.Status403Forbidden)
            {
                response.Headers.Add("www-authenticate", "Bearer");

                var unauthorizedResponse = new
                {
                    status = _status == 401 ? 401 : 403,
                    traceId = _traceId,
                    message = _status == 401 ? "Unauthorized" : "Forbidden"
                };

                var json = JsonConvert.SerializeObject(unauthorizedResponse);
                await response.WriteAsync(json);
                return;
            }

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = _status,
                traceId = _traceId,
                title = _title,
                error = _errorDetail
            });

            await response.WriteAsync(jsonResponse);
        }
    }
}
