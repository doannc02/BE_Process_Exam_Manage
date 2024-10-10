using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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
                var exams = await _examRepository.GetListExamsAsync(examRequest);

                if (exams != null && exams.content.Any())
                {
                    var response = _createCommon.CreateResponse("Lấy danh sách bài thi thành công", HttpContext, exams);
                    return Ok(response);
                }
                else
                {
                    return new CustomJsonResult(404, HttpContext, "Không tìm thấy bài thi nào");
                }
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
        public async Task<IActionResult> PostExamAsync([FromBody] List<ExamDTO> examDTOs)
        {
            try
            {
                var createExam = await _examRepository.CreateExamsAsync(examDTOs);

                if (createExam != null && createExam.data != null)
                {
                    var response = _createCommon.CreateResponse(createExam.message, HttpContext, createExam.data);
                    return Ok(response);
                }
                else
                {
                    var response = _createCommon.CreateResponse(createExam.message, HttpContext, createExam);
                    return Ok(response);
                }
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
                var updateExam = await _examRepository.UpdateExamAsync(examDTO);

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

        [HttpPut("update-state")]
        public async Task<IActionResult> UpdateStateAsync([Required] int examId, [Required] string status, string? comment)
        {
            try
            {
                var updateStatus = await _examRepository.UpdateStateAsync(examId, status, comment);

                if (updateStatus != null && updateStatus.data != null)
                {
                    var response = _createCommon.CreateResponse(updateStatus.message, HttpContext, updateStatus.data);
                    return Ok(response);
                }
                else if (updateStatus != null)
                {
                    return new CustomJsonResult((int)updateStatus.status, HttpContext, updateStatus.message, (List<ErrorDetail>)updateStatus.errors);
                }
                else
                {
                    return new CustomJsonResult(400, HttpContext, "Error");
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
