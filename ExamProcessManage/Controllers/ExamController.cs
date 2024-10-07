using ExamProcessManage.Dtos;
using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.RequestModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        // GET: api/<ValuesController>
        [HttpGet("list")]
        public async Task<IActionResult> GetListExamAsync([FromQuery] ExamRequestParams examRequest)
        {
            return Ok(200);
        }

        // GET api/<ValuesController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailExamAsync(int id)
        {
            return Ok(200);
        }

        // POST api/<ValuesController>
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

        // PUT api/<ValuesController>/5
        [HttpPut]
        public async Task<IActionResult> PutExamAsync(int id, [FromBody] string value)
        {
            return Ok();
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete]
        public async Task<IActionResult> DeleteExamAsync(int id)
        {
            return Ok();
        }
    }
}
