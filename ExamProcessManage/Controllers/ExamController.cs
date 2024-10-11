using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/exam")]
    [ApiController]
    [AllowAnonymous]
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
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");
                
                if (roleClaim == null || userId == null)
                {
                    return Forbid();
                }
                if(userId != null && roleClaim.Value != "Admin")
                {
                    var exams1 = await _examRepository.GetListExamsAsync(examRequest, int.Parse(userId.Value));

                   var res  = _createCommon.CreateResponse("Lấy danh sách bài thi thành công", HttpContext, exams1);
                    return Ok(res);
                }
                var exams = await _examRepository.GetListExamsAsync(examRequest, null);

                var response = _createCommon.CreateResponse("Lấy danh sách bài thi thành công", HttpContext, exams);
                return Ok(response);
                
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error: " + ex.Message);
            }
        }

        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailExamAsync([Required] int examId)
        {
            try
            {
                var examDetail = await _examRepository.GetDetailExamAsync(examId);

                if (examDetail != null)
                {
                    var response = _createCommon.CreateResponse("Lấy chi tiết bài thi thành công", HttpContext, examDetail);
                    return Ok(response);
                }
                else
                {
                    return new CustomJsonResult(404, HttpContext, $"Không tìm thấy bài thi với ID {examId}");
                }
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostExamAsync([FromBody] IEnumerable<ExamDTO> examDTOs)
        {
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if(roleClaim == null) {
                    return Forbid();
                }
                if(userId != null && roleClaim.Value != "Admin")
                {
                    var createExam = await _examRepository.CreateExamsAsync(examDTOs.ToList(), int.Parse(userId.Value));

                    if (createExam != null && createExam.data != null)
                    {
                        var response = _createCommon.CreateResponse(createExam.message, HttpContext, createExam.data);
                        return Ok(response);
                    }
                    else
                    {
                        var response = _createCommon.CreateResponse(createExam.message, HttpContext, createExam);
                        return BadRequest(response);
                    }
                }
                return Unauthorized();

            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error");
            }
        }

        [HttpPut]
        public async Task<IActionResult> PutExamAsync([FromBody] ExamDTO examDTO)
        {
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null)
                {
                    return Forbid();
                }
                if (userId != null && roleClaim.Value != "Admin")
                {
                    var updateExam = await _examRepository.UpdateExamAsync(examDTO, int.Parse(userId.Value));

                    if (updateExam != null && updateExam.data != null)
                    {
                        var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam.data);
                        return Ok(response);
                    }
                    else
                    {
                        var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam);
                        return Ok(response);
                    }
                }
                return Unauthorized();


               
            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error");
            }
        }

        [HttpPut("update-state")]
        public async Task<IActionResult> UpdateStateAsync([Required] int examId, [Required] string status, string? comment)
        {
            try
            {
                var updateExam = await _examRepository.UpdateStateAsync(examId, status, comment);

                if (updateExam != null && updateExam.data != null)
                {
                    var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam.data);
                    return Ok(response);
                }
                else
                {
                    var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam);
                    return Ok(response);
                }
            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error");
            }
        }

        [HttpPut("remove-exam")]
        public async Task<IActionResult> RemoveExamAsync([Required] int examSetId, [Required] int examId, string? comment)
        {
            try
            {
                var updateExam = await _examRepository.RemoveChildAsync(examSetId, examId, comment);

                if (updateExam != null && updateExam.data != null)
                {
                    var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam.data);
                    return Ok(response);
                }
                else
                {
                    var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam);
                    return Ok(response);
                }
            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExamAsync(int id)
        {
            try
            {
                var deleteExam = await _examRepository.DeleteExamAsync(id);

                if (deleteExam != null && deleteExam.data != null)
                {
                    var response = _createCommon.CreateResponse(deleteExam.message, HttpContext, deleteExam.data);
                    return Ok(response);
                }
                else
                {
                    var response = _createCommon.CreateResponse(deleteExam.message, HttpContext, deleteExam);
                    return Ok(response);
                }
            }
            catch
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error");
            }
        }
    }
}
