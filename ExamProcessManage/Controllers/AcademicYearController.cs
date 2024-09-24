using ExamProcessManage.Helpers;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ExamProcessManage.Controllers
{
    [Route("api/v1/academicyear")]
    [ApiController]
    public class AcademicYearController : ControllerBase
    {
        private readonly IAcademicYearRepository _repository;
        private readonly CreateCommonResponse _createCommon;

        public AcademicYearController(IAcademicYearRepository repository)
        {
            _repository = repository;
            _createCommon = new CreateCommonResponse();
        }

        // GET: list of academic_years
        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetListAcademicYear([FromQuery] QueryObject queryObject)
        {
            var academics = await _repository.GetListAcademicYearAsync(queryObject);

            if (academics.content.Any())
            {
                var commonResponse = _createCommon.CreateResponse("Success", HttpContext, academics);
                return Ok(commonResponse);
            }
            else
            {
                return BadRequest();
            }
        }

        // GET api/<AcademicYearController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AcademicYearController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<AcademicYearController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AcademicYearController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
