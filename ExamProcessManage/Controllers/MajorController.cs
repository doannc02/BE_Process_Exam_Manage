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
        public async Task<IActionResult> GetListMajorAsync([FromQuery] int departmentId,
            [FromQuery] QueryObject queryObject)
        {
            var listMajors = await _majorRepository.GetListMajorAsync(departmentId, queryObject);

            var commonResponse = _createCommonResponse.CreateResponse("Thành công", HttpContext, listMajors);
            return Ok(commonResponse);
        }

        // GET api/<MajorController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailMajorAsync([FromQuery] [Required] int id)
        {
            var major = await _majorRepository.GetDetailMajorAsync(id);

            if (major.status != 200)
                return new CustomJsonResult(major.status, HttpContext, major.message, major.errors);

            var response = _createCommonResponse.CreateResponse(major.message, HttpContext, major.data);
            return Ok(response);
        }

        // POST api/<MajorController>
        [HttpPost]
        public async Task<IActionResult> PostMajorAsync([FromBody] MajorResponse inputMajor)
        {
            var newMajor = await _majorRepository.CreateMajorAsync(inputMajor);

            if (newMajor.status != 200)
                return new CustomJsonResult(newMajor.status, HttpContext, newMajor.message, newMajor.errors);

            var response = _createCommonResponse.CreateResponse(newMajor.message, HttpContext, newMajor.data);
            return Ok(response);
        }

        // PUT api/<MajorController>/5
        [HttpPut]
        public async Task<IActionResult> PutMajorAsync([FromBody] MajorResponse inputMajor)
        {
            var updatedMajor = await _majorRepository.UpdateMajorAsync(inputMajor);

            if (updatedMajor.status != 200)
                return new CustomJsonResult(updatedMajor.status, HttpContext, updatedMajor.message,
                    updatedMajor.errors);

            var response =
                _createCommonResponse.CreateResponse(updatedMajor.message, HttpContext, updatedMajor.data);
            return Ok(response);
        }

        // DELETE api/<MajorController>/5
        [HttpDelete]
        public async Task<IActionResult> DeleteMajorAsync([FromQuery] [Required] int id)
        {
            var delMajor = await _majorRepository.DeleteMajorAsync(id);

            if (delMajor.status != 200)
                return new CustomJsonResult(delMajor.status, HttpContext, delMajor.message, delMajor.errors);

            var response = _createCommonResponse.CreateResponse(delMajor.message, HttpContext, delMajor.data);
            return Ok(response);
        }
    }
}