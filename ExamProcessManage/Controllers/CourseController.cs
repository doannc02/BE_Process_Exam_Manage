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

        private const string Success = "Success";

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
            try
            {
                var listCourse = await _repository.GetListCourseAsync(majorId, queryObject);

                var commonResponse = _createCommon.CreateResponse(Success, HttpContext, listCourse);
                return Ok(commonResponse);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }

        // GET api/<ValuesController>/5
        [HttpGet("detail")]
        public async Task<IActionResult> GetDetailCourseAsync([FromQuery] [Required] int id)
        {
            try
            {
                var course = await _repository.GetDetailCourseAsync(id);

                if (course is { data: not null })
                {
                    return new CustomJsonResult(404, HttpContext, course.message!);
                }

                var response = _createCommon.CreateResponse(Success, HttpContext, course.data);
                return Ok(response);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }

        // POST api/<ValuesController>
        [HttpPost]
        public async Task<IActionResult> PostCourseAsync([FromBody] List<CourseReponse> inputCourses)
        {
            try
            {
                if (inputCourses.Any() != true)
                {
                    return new CustomJsonResult(400, HttpContext, "No course data provided");
                }

                if (inputCourses.Any(inputCourse =>
                        inputCourse.id == 0 || inputCourse.name == "string" || inputCourse.major.id == 0))
                {
                    return new CustomJsonResult(400, HttpContext, "Invalid for one or more courses");
                }

                var newCourseResponse = await _repository.CreateCourseAsync(inputCourses);

                var response =
                    _createCommon.CreateResponse(newCourseResponse.message!, HttpContext, newCourseResponse.data);
                return Ok(response);
            }
            catch (Exception e)
            {
                return new CustomJsonResult(500, HttpContext, "Internal Server Error", new List<ErrorDetail>
                {
                    new()
                    {
                        message = $"{e.Message}: {e.InnerException?.Message}"
                    }
                });
            }
        }

        // PUT api/<ValuesController>/5
        [HttpPut]
        public async Task<IActionResult> PutCourseAsync([FromBody] CourseReponse inputCourse)
        {
            if (inputCourse != null && inputCourse.id != 0 && inputCourse.name != "string")
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
        public async Task<IActionResult> DeleteCourseAsync([FromQuery] [Required] int id)
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