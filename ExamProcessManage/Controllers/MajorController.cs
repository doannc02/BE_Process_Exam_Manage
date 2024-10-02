using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/major")]
    [ApiController]
    public class MajorController : ControllerBase
    {
        private readonly IMajorRepository _majorRepository;
        private readonly CreateCommonResponse _createCommonResponse;

        public MajorController(IMajorRepository majorRepository)
        {
            _majorRepository = majorRepository;
            _createCommonResponse = new CreateCommonResponse();
        }

        // GET: api/<MajorController>
        [HttpGet("list")]
        public async Task<IActionResult> GetListMajorAsync([FromQuery] int departmentId, [FromQuery] QueryObject queryObject)
        {
            var listMajors = await _majorRepository.GetListMajorAsync(departmentId, queryObject);

            if (listMajors.content.Any())
            {
                var commonResponse = _createCommonResponse.CreateResponse("success", HttpContext, listMajors);
                return Ok(commonResponse);
            }
            else if (departmentId > 0 && !listMajors.content.Any())
            {
                return new CustomJsonResult(404, HttpContext, $"no major with department_id = '{departmentId}'");
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "bad request");
            }
        }

        // GET api/<MajorController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailMajorAsync([FromQuery][Required] int id)
        {
            var major = await _majorRepository.GetDetailMajorAsync(id);

            if (major != null && major.data != null)
            {
                var response = _createCommonResponse.CreateResponse(major.message, HttpContext, major.data);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, major.message);
            }
        }

        // POST api/<MajorController>
        [HttpPost]
        public async Task<IActionResult> PostMajorAsync([FromBody] MajorResponse inputMajor)
        {
            if (inputMajor != null && inputMajor.id > 0 &&
                inputMajor.name != "string" && inputMajor.department.id != 0)
            {
                var newCourse = await _majorRepository.CreateMajorAsync(inputMajor);

                if (newCourse.data != null)
                {
                    var response = _createCommonResponse.CreateResponse(newCourse.message, HttpContext, newCourse.data);
                    return Ok(response);
                }
                else
                {
                    return new CustomJsonResult(409, HttpContext, newCourse.message);
                }
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "invalid input");
            }
        }

        // PUT api/<MajorController>/5
        [HttpPut]
        public async Task<IActionResult> PutMajorAsync([FromBody] MajorResponse inputMajor)
        {
            if (inputMajor != null && inputMajor.id > 0 && inputMajor.name != "string")
            {
                var updatedMajor = await _majorRepository.UpdateMajorAsync(inputMajor);

                if (updatedMajor.data != null)
                {
                    var response = _createCommonResponse.CreateResponse(updatedMajor.message, HttpContext, updatedMajor.data);
                    return Ok(response);
                }
                else if (updatedMajor.message.Contains("no changes"))
                {
                    return new CustomJsonResult(418, HttpContext, updatedMajor.message);
                }
                else
                {
                    return new CustomJsonResult(404, HttpContext, updatedMajor.message);
                }
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "invalid input");
            }
        }

        // DELETE api/<MajorController>/5
        [HttpDelete]
        public async Task<IActionResult> DeleteMajorAsync([FromQuery][Required] int id)
        {
            var delMajor = await _majorRepository.DeleteMajorAsync(id);

            if (delMajor.data != null)
            {
                var response = _createCommonResponse.CreateResponse(delMajor.message, HttpContext, delMajor.data);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, delMajor.message);
            }
        }
    }
}
