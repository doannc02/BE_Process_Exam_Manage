using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

namespace ExamProcessManage.Helpers
{
    public class CustomJsonResult : IActionResult
    {
        private readonly int _status;
        private readonly string _traceId;
        private readonly string _title;

        public CustomJsonResult(int status, HttpContext httpContext, string title)
        {
            _status = status;
            _traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            _title = title;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = "application/json";
            response.Headers["Content-Type"] = "application/json";
            response.StatusCode = _status;

            var result = new
            {
                error = new
                {
                    status = _status,
                    title = _title,
                    traceId = _traceId
                }
            };
            var json = JsonConvert.SerializeObject(result);
            if (_status == StatusCodes.Status401Unauthorized)
            {
                response.Headers.Add("www-authenticate", "Bearer");
                response.Headers["Content-Type"] = "application/json";
            }

            if (_status == StatusCodes.Status403Forbidden)
            {
                response.Headers.Add("www-authorized", "Bearer");
                response.Headers["Content-Type"] = "application/json";
            }

            await response.WriteAsync(json);
        }
    }
}
