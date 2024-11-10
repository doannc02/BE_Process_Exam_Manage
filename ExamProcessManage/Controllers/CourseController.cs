using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/course")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly ICourseRepository _repository;
        private readonly CreateCommonResponse _createCommon;

        public CourseController(ICourseRepository courseRepository)
        {
            _repository = courseRepository;
            _createCommon = new CreateCommonResponse();
        }

        // GET: api/<ValuesController>
        [HttpGet("list")]
        public async Task<IActionResult> GetListCourseAsync([FromQuery] int majorId,
            [FromQuery] QueryObject queryObject)
        {
            var listCourse = await _repository.GetListCourseAsync(majorId, queryObject);

            var commonResponse = _createCommon.CreateResponse("success", HttpContext, listCourse);
            return Ok(commonResponse);
        }

        // GET api/<ValuesController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailCourseAsync([FromQuery] [Required] int id)
        {
            var course = await _repository.GetDetailCourseAsync(id);

            if (course.status != 200)
                return new CustomJsonResult(course.status, HttpContext, course.message, course.errors);

            var response = _createCommon.CreateResponse(course.message, HttpContext, course.data);
            return Ok(response);
        }

        // POST api/<ValuesController>
        [HttpPost]
        public async Task<IActionResult> PostCourseAsync([FromBody] List<CourseResponse> inputCourses)
        {
            var course = await _repository.CreateCourseAsync(inputCourses);

            if (course.status != 200)
                return new CustomJsonResult(course.status, HttpContext, course.message, course.errors);

            var response = _createCommon.CreateResponse(course.message, HttpContext, course.data);
            return Ok(response);
        }

        // PUT api/<ValuesController>/5
        [HttpPut]
        public async Task<IActionResult> PutCourseAsync([FromBody] CourseResponse inputCourse)
        {
            var updatedCourse = await _repository.UpdateCourseAsync(inputCourse);

            if (updatedCourse.status != 200)
                return new CustomJsonResult(updatedCourse.status, HttpContext, updatedCourse.message,
                    updatedCourse.errors);

            var response = _createCommon.CreateResponse(updatedCourse.message, HttpContext, updatedCourse.data);
            return Ok(response);
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete]
        public async Task<IActionResult> DeleteCourseAsync([FromQuery] [Required] int id)
        {
            var delCourse = await _repository.DeleteCourseAsync(id);

            if (delCourse.status != 200)
                return new CustomJsonResult(delCourse.status, HttpContext, delCourse.message,
                    delCourse.errors);

            var response = _createCommon.CreateResponse(delCourse.message, HttpContext, delCourse.data);
            return Ok(response);
        }
    }
}