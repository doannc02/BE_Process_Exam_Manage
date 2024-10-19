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
            try
            {
                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");

                if (roleClaim == null || userId == null)
                {
                    return Forbid();
                }
                if (userId != null && roleClaim.Value != "Admin")
                {
                    var exams1 = await _examRepository.GetListExamsAsync(examRequest, int.Parse(userId.Value));

                    var res = _createCommon.CreateResponse("Lấy danh sách bài thi thành công", HttpContext, exams1);
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

                if (examDetail.data != null)
                {
                    var response = _createCommon.CreateResponse(examDetail.message, HttpContext, examDetail.data);
                    return Ok(response);
                }
                else
                {
                    return new CustomJsonResult((int)examDetail.status, HttpContext, examDetail.message, examDetail.errors);
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

                if (roleClaim == null)
                {
                    return Forbid();
                }
                if (userId != null && roleClaim.Value != "Admin")
                {
                    var createExam = await _examRepository.CreateExamsAsync(examDTOs.ToList(), int.Parse(userId.Value));

                    if (createExam != null && createExam.data != null)
                    {
                        var response = _createCommon.CreateResponse(createExam.message, HttpContext, createExam.data);
                        return Ok(response);
                    }
                    else
                    {
                        return new CustomJsonResult((int)createExam.status, HttpContext, createExam.message, createExam.errors);
                    }
                }
                return Unauthorized();

            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, $"Internal Server Error: {ex.Message} {ex.InnerException}");
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
                if (userId != null)
                {
                    var updateExam = await _examRepository.UpdateExamAsync(int.Parse(userId.Value), roleClaim.Value == "Admin", examDTO);

                    if (updateExam != null && updateExam.data != null)
                    {
                        var response = _createCommon.CreateResponse(updateExam.message, HttpContext, updateExam.data);
                        return Ok(response);
                    }
                    else
                    {
                        return new CustomJsonResult((int)updateExam.status, HttpContext, updateExam.message, updateExam.errors);
                    }
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, $"Internal Server Error: {ex.Message} {ex.InnerException}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExamAsync(int id)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == "userId");
                if (userId == null) return new CustomJsonResult(405, HttpContext, "User not authenticated");

                var deleteExam = await _examRepository.DeleteExamAsync(int.Parse(userId.Value), id);
                if (deleteExam == null) return new CustomJsonResult(500, HttpContext, "An error occurred!");

                if (deleteExam.data != null)
                    return Ok(_createCommon.CreateResponse(deleteExam.message, HttpContext, deleteExam.data));
                else
                    return new CustomJsonResult((int)deleteExam.status, HttpContext, deleteExam.message, deleteExam.errors);
            }
            catch (Exception ex)
            {
                return new CustomJsonResult(500, HttpContext, $"Internal Server Error: {ex.Message} {ex.InnerException}");
            }
        }
    }
}
