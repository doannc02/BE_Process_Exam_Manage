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
            response.StatusCode = _status;

            var result = new
            {
                status = _status,
                title = _title,
                traceId = _traceId
            };

            var json = JsonConvert.SerializeObject(result);
            await response.WriteAsync(json);
        }
    }
}
