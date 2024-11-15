using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/exam")]
    [ApiController]
    public class ExamController : ControllerBase
    {
        private readonly IExamRepository _examRepository;
        private readonly CreateCommonResponse _createCommon;

        public ExamController(IExamRepository examRepository)
        {
            _examRepository = examRepository;
            _createCommon = new CreateCommonResponse();
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetListExamAsync([FromQuery] ExamRequestParams examRequest)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

            if (roleClaim == null || userId == null)
                return Forbid();

            var exams = await _examRepository.GetListExamsAsync(examRequest,
                roleClaim.Value == "Admin" ? null : int.Parse(userId.Value));

            var response = _createCommon.CreateResponse("Thành công", HttpContext, exams);
            return Ok(response);
        }

        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailExamAsync([Required] int examId)
        {
            var examDetail = await _examRepository.GetDetailExamAsync(examId);

            if (examDetail.status != 200)
                return new CustomJsonResult(examDetail.status, HttpContext, examDetail.message,
                    examDetail.errors);

            var response = _createCommon.CreateResponse(examDetail.message, HttpContext, examDetail.data);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> PostExamAsync([FromBody] IEnumerable<ExamDTO> examDtOs)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

            if (roleClaim == null || userId == null || roleClaim.Value == "Admin")
                return Forbid();

            var createExam = await _examRepository.CreateExamsAsync(examDtOs.ToList(), int.Parse(userId.Value));

            if (createExam.status != 200)
                return new CustomJsonResult(createExam.status, HttpContext, createExam.message, createExam.errors);

            var response = _createCommon.CreateResponse(createExam.message, HttpContext, createExam.data);
            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> PutExamAsync([FromBody] ExamDTO examDto)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

            if (roleClaim == null || userId == null)
                return Forbid();

            var updateExam = await _examRepository.UpdateExamAsync(int.Parse(userId.Value),
                roleClaim.Value == "Admin", examDto);

            if (updateExam.status != 200)
                return new CustomJsonResult(updateExam.status, HttpContext, updateExam.message, updateExam.errors);

            var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam.data);
            return Ok(response);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteExamAsync([FromQuery] [Required] int id)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");
            if (userId == null)
                return Forbid();

            var deleteExam = await _examRepository.DeleteExamAsync(int.Parse(userId.Value), id);

            if (deleteExam.status != 200)
                return new CustomJsonResult(deleteExam.status, HttpContext, deleteExam.message, deleteExam.errors);

            var response = _createCommon.CreateResponse(deleteExam.message, HttpContext, deleteExam.data);
            return Ok(response);
        }
    }
}