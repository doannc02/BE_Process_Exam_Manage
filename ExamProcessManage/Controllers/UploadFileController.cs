using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/upload-file")]
    [ApiController]
    public class UploadFileController : ControllerBase
    {
        private readonly IUploadFileService _uploadFileService;
        public UploadFileController(IUploadFileService uploadFileService)
        {
            _uploadFileService = uploadFileService;
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }
            var url = await _uploadFileService.UploadFile(file);

            if(url == null)
            {
                return new CustomJsonResult(500, HttpContext, "Upload lỗi!");
            }
            return Ok(url);
        }
    }
}
