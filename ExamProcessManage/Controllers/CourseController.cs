using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
        [AllowAnonymous]
        public async Task<IActionResult> GetListCourseAsync([FromQuery] QueryObject queryObject)
        {
            var listCourse = await _repository.GetListCourseAsync(queryObject);
            var response = _createCommon.CreateResponse("", HttpContext, listCourse);
            return Ok(response);
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ValuesController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
