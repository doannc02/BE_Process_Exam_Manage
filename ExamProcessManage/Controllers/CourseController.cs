using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.ResponseModels;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/course")]
    [ApiController]
    [AllowAnonymous]
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
        public async Task<IActionResult> GetListCourseAsync([FromQuery] int majorId, [FromQuery] QueryObject queryObject)
        {
            var listCourse = await _repository.GetListCourseAsync(majorId, queryObject);

            if (listCourse.content.Any())
            {
                var commonResponse = _createCommon.CreateResponse("success", HttpContext, listCourse);
                return Ok(commonResponse);
            }
            else if (majorId > 0 && !listCourse.content.Any())
            {
                return new CustomJsonResult(404, HttpContext, $"no course with majorid = '{majorId}'");
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "bad request");
            }
        }

        // GET api/<ValuesController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailCourseAsync([FromQuery][Required] int id)
        {
            var course = await _repository.GetDetailCourseAsync(id);

            if (course != null && course.data != null)
            {
                var response = _createCommon.CreateResponse(course.message, HttpContext, course.data);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, course.message);
            }
        }

        // POST api/<ValuesController>
        [HttpPost]
        public async Task<IActionResult> PostCourseAsync([FromBody] CourseReponse inputCourse)
        {
            if (inputCourse != null && inputCourse.course_id != 0 &&
                inputCourse.course_name != "string" && inputCourse.major.id != 0)
            {
                var newCourse = await _repository.CreateCourseAsync(inputCourse);

                if (newCourse.data != null)
                {
                    var response = _createCommon.CreateResponse(newCourse.message, HttpContext, newCourse.data);
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

        // PUT api/<ValuesController>/5
        [HttpPut]
        public async Task<IActionResult> PutCourseAsync([FromBody] CourseReponse inputCourse)
        {
            if (inputCourse != null && inputCourse.course_id != 0 && inputCourse.course_name != "string")
            {
                var updatedCourse = await _repository.UpdateCourseAsync(inputCourse);

                if (updatedCourse.data != null)
                {
                    var response = _createCommon.CreateResponse(updatedCourse.message, HttpContext, updatedCourse.data);
                    return Ok(response);
                }
                else if (updatedCourse.message.Contains("no changes"))
                {
                    return new CustomJsonResult(418, HttpContext, updatedCourse.message);
                }
                else
                {
                    return new CustomJsonResult(404, HttpContext, updatedCourse.message);
                }
            }
            else
            {
                return new CustomJsonResult(400, HttpContext, "invalid input");
            }
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete]
        public async Task<IActionResult> DeleteCourseAsync(int id)
        {
            var delCourse = await _repository.DeleteCourseAsync(id);

            if (delCourse.data != null)
            {
                var response = _createCommon.CreateResponse(delCourse.message, HttpContext, delCourse.data);
                return Ok(response);
            }
            else
            {
                return new CustomJsonResult(404, HttpContext, delCourse.message);
            }
        }
    }
}
