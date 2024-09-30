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
        public async Task<IActionResult> GetListCourseAsync([FromQuery] QueryObject queryObject)
        {
            var listCourse = await _repository.GetListCourseAsync(queryObject);

            if (listCourse.content.Any())
            {
                var commonResponse = _createCommon.CreateResponse("success", HttpContext, listCourse);
                return Ok(commonResponse);
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

                if (newCourse.data!= null)
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
        public void PutCourseAsync(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete]
        public void DeleteCourseAsync(int id)
        {
        }
    }
}
