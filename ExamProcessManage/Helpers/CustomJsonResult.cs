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

            // Build the base response object with errors (can be null)
            var jsonResponse = new
            {
                status = _status,
                traceId = _traceId,
                title = _title,
                errors = _errorDetail
            };

            // Customize response for specific status codes
            switch (_status)
            {
                case StatusCodes.Status401Unauthorized:
                    response.Headers.Add("www-authenticate", "Bearer");
                    jsonResponse = new
                    {
                        status = 401,
                        traceId = _traceId,
                        title = "Unauthorized",
                        errors = (List<ErrorDetail>?)null // Ensure consistent structure
                    };
                    break;

                case StatusCodes.Status403Forbidden:
                    response.Headers.Add("www-authenticate", "Bearer");
                    jsonResponse = new
                    {
                        status = 403,
                        traceId = _traceId,
                        title = "Forbidden",
                        errors = _errorDetail // Use the actual error details if any
                    };
                    break;
            }

            // Serialize and write the response
            await response.WriteAsJsonAsync(jsonResponse);
        }
    }
}
